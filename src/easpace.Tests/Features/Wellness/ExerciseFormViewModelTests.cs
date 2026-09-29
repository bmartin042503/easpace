// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.ViewModels;
using easpace.Desktop.Services.Core;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness;

public class ExerciseFormViewModelTests
{
    private static BreathingExercise Breathing() => new()
    {
        Name = "Box",
        Description = "Square breathing",
        IsRepeating = true,
        Instructions =
        [
            // stored out of order on purpose
            new ExerciseInstruction { Order = 2, DurationSeconds = 4, Phase = BreathingPhaseType.HoldIn },
            new ExerciseInstruction { Order = 1, DurationSeconds = 4, Phase = BreathingPhaseType.Inhale, Text = "In" },
            new ExerciseInstruction { Order = 3, DurationSeconds = 4, Phase = BreathingPhaseType.Exhale }
        ]
    };

    private static MeditationExercise Meditation() => new()
    {
        Name = "Calm",
        Instructions = [new ExerciseInstruction { Order = 1, DurationSeconds = 60, Text = "Sit" }]
    };

    private static IEnumerable<string?> ErrorKeys(ExerciseFormViewModel form) =>
        form.GetErrors().Select(e => e.ErrorMessage)
            .Concat(form.Instructions.SelectMany(i => i.GetErrors()).Select(e => e.ErrorMessage))
            .Append(form.InstructionsError)
            .Where(key => key is not null);

    private static void SetPhase(ExerciseInstructionViewModel instruction, BreathingPhaseType? phase) =>
        instruction.SelectedPhase = instruction.PhaseOptions.Single(o => o.Value == phase);

    [Fact]
    public void Constructor_LoadsTheExerciseInInstructionOrder()
    {
        var exercise = Breathing();

        var form = new ExerciseFormViewModel(exercise);

        form.Id.Should().Be(exercise.Id);
        form.Type.Should().Be(WellnessSessionType.Breathing);
        form.IsCreatingNew.Should().BeFalse();
        form.Name.Should().Be("Box");
        form.Description.Should().Be("Square breathing");
        form.IsRepeating.Should().BeTrue();
        form.Instructions.Select(i => (i.Number, i.Text, i.DurationSeconds, i.Phase)).Should().Equal(
            (1, "In", 4, BreathingPhaseType.Inhale),
            (2, "", 4, BreathingPhaseType.HoldIn),
            (3, "", 4, BreathingPhaseType.Exhale));
        form.TotalCycleSeconds.Should().Be(12);
        form.OneCycleDurationText.Should().Be(string.Format(LocalizationService.GetString("Wellness.Editor.Label.OneCycle"), "00:12"));
        form.IsDirty.Should().BeFalse();
        form.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithMeditation_DropsPhases()
    {
        var exercise = Meditation();
        exercise.Instructions.Single().Phase = BreathingPhaseType.Inhale;

        var form = new ExerciseFormViewModel(exercise);

        var instruction = form.Instructions.Single();
        instruction.SupportsPhases.Should().BeFalse();
        instruction.Phase.Should().BeNull();
        form.IsDirty.Should().BeFalse();
    }

    [Theory]
    [InlineData("blank name", "FormValidation.Name.Required")]
    [InlineData("name too long", "FormValidation.Name.MaxLength")]
    [InlineData("description too long", "FormValidation.Description.MaxLength")]
    [InlineData("text too long", "FormValidation.InstructionText.MaxLength")]
    [InlineData("no text and no phase", "FormValidation.InstructionText.Required")]
    [InlineData("zero duration", "FormValidation.Duration.Range")]
    [InlineData("duration too long", "FormValidation.Duration.Range")]
    [InlineData("no instructions", "FormValidation.Instructions.Required")]
    [InlineData("cycle too long", "FormValidation.Instructions.TooLong")]
    public void InvalidValues_ReportTheirErrorAndMakeTheFormInvalid(string invalidCase, string expectedKey)
    {
        var form = new ExerciseFormViewModel(Breathing());
        var first = form.Instructions[0];

        switch (invalidCase)
        {
            case "blank name": form.Name = "   "; break;
            case "name too long": form.Name = new string('a', 65); break;
            case "description too long": form.Description = new string('a', 257); break;
            case "text too long": first.Text = new string('a', 257); break;
            case "no text and no phase": first.Text = ""; SetPhase(first, null); break;
            case "zero duration": first.DurationSeconds = 0; break;
            case "duration too long": first.DurationSeconds = 3601; break;
            case "no instructions": form.Instructions.Clear(); break;
            case "cycle too long": first.DurationSeconds = 1800; form.Instructions[1].DurationSeconds = 1801; break;
        }

        ErrorKeys(form).Should().Contain(expectedKey);
        form.IsValid.Should().BeFalse();
    }

    [Fact]
    public void BoundaryValues_AreValid()
    {
        var form = new ExerciseFormViewModel(Breathing());

        form.Name = new string('a', 64);
        form.Description = new string('a', 256);
        form.Instructions[0].Text = new string('a', 256);
        form.Instructions[0].DurationSeconds = 3592;

        form.TotalCycleSeconds.Should().Be(3600);
        form.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InstructionWithPhase_UsesThePhaseTextAsHintAndDoesNotNeedText()
    {
        var instruction = new ExerciseFormViewModel(Breathing()).Instructions[1];

        instruction.Text.Should().BeEmpty();
        instruction.HasErrors.Should().BeFalse();
        instruction.TextPlaceholder.Should().Be(ExerciseStep.GetDefaultText(BreathingPhaseType.HoldIn));

        SetPhase(instruction, null);

        instruction.TextPlaceholder.Should().Be(LocalizationService.GetString("Wellness.Editor.Input.TextPlaceholder"));
        instruction.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void SelectedPhase_IgnoresNull()
    {
        var instruction = new ExerciseFormViewModel(Breathing()).Instructions[0];

        instruction.SelectedPhase = null;

        instruction.Phase.Should().Be(BreathingPhaseType.Inhale);
    }

    [Fact]
    public void AddInstruction_WithBreathing_ContinuesThePhaseRotation()
    {
        var form = new ExerciseFormViewModel(Breathing());

        form.AddInstructionCommand.Execute(null);
        form.AddInstructionCommand.Execute(null);
        form.AddInstructionCommand.Execute(null);

        form.Instructions.Skip(3).Select(i => (i.Number, i.Text, i.DurationSeconds, i.Phase)).Should().Equal(
            (4, "", 4, BreathingPhaseType.HoldOut),
            (5, "", 4, BreathingPhaseType.Inhale),
            (6, "", 4, BreathingPhaseType.HoldIn));
        form.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddInstruction_WithMeditation_AddsAMinuteThatNeedsText()
    {
        var form = new ExerciseFormViewModel(Meditation());

        form.AddInstructionCommand.Execute(null);

        var added = form.Instructions[1];
        (added.Number, added.Text, added.DurationSeconds, added.Phase).Should().Be((2, "", 60, null));
        form.IsValid.Should().BeFalse();

        added.Text = "Breathe";

        form.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RemoveInstruction_RenumbersTheOthers()
    {
        var form = new ExerciseFormViewModel(Breathing());

        form.RemoveInstructionCommand.Execute(form.Instructions[0]);

        form.Instructions.Select(i => (i.Number, i.Phase)).Should().Equal(
            (1, BreathingPhaseType.HoldIn),
            (2, BreathingPhaseType.Exhale));
        form.TotalCycleSeconds.Should().Be(8);
    }

    [Fact]
    public void MoveInstruction_ReordersAndRenumbers()
    {
        var form = new ExerciseFormViewModel(Breathing());
        var exhale = form.Instructions[2];

        form.MoveInstructionUpCommand.Execute(exhale);

        form.Instructions.Select(i => (i.Number, i.Phase)).Should().Equal(
            (1, BreathingPhaseType.Inhale),
            (2, BreathingPhaseType.Exhale),
            (3, BreathingPhaseType.HoldIn));

        form.MoveInstructionDownCommand.Execute(form.Instructions[0]);

        form.Instructions.Select(i => i.Phase).Should().Equal(
            BreathingPhaseType.Exhale, BreathingPhaseType.Inhale, BreathingPhaseType.HoldIn);
    }

    [Fact]
    public void MoveInstruction_CannotLeaveTheList()
    {
        var form = new ExerciseFormViewModel(Breathing());
        var first = form.Instructions[0];
        var last = form.Instructions[^1];

        form.MoveInstructionUpCommand.CanExecute(first).Should().BeFalse();
        form.MoveInstructionDownCommand.CanExecute(last).Should().BeFalse();
        form.MoveInstructionUpCommand.CanExecute(last).Should().BeTrue();
        form.MoveInstructionDownCommand.CanExecute(first).Should().BeTrue();

        form.MoveInstructionUpCommand.Execute(first);

        form.Instructions[0].Should().BeSameAs(first);
    }

    [Fact]
    public void ToRequest_MapsTheFieldsAndInstructionsInOrder()
    {
        var exercise = Breathing();
        var form = new ExerciseFormViewModel(exercise)
        {
            Name = "  Renamed  ",
            Description = "Changed",
            IsRepeating = false
        };
        form.MoveInstructionDownCommand.Execute(form.Instructions[0]);
        form.Instructions[2].DurationSeconds = 6;

        var request = form.ToRequest();

        request.Should().BeEquivalentTo(new UpsertWellnessExerciseRequest(exercise.Id, WellnessSessionType.Breathing,
            "  Renamed  ", "Changed", false,
            [
                new UpsertExerciseInstructionRequest("", 4, BreathingPhaseType.HoldIn),
                new UpsertExerciseInstructionRequest("In", 4, BreathingPhaseType.Inhale),
                new UpsertExerciseInstructionRequest("", 6, BreathingPhaseType.Exhale)
            ]), options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToSteps_ResolvesPhaseTextsAndSkipsInstructionsThatCannotRun()
    {
        var form = new ExerciseFormViewModel(Breathing());
        form.Instructions[2].Text = "";
        SetPhase(form.Instructions[2], null);

        var steps = form.ToSteps();

        steps.Should().Equal(
            ExerciseStep.Create("In", 4, BreathingPhaseType.Inhale),
            ExerciseStep.Create(null, 4, BreathingPhaseType.HoldIn));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("repeat")]
    [InlineData("text")]
    [InlineData("duration")]
    [InlineData("phase")]
    [InlineData("add")]
    [InlineData("remove")]
    [InlineData("move")]
    public void EachChange_MakesTheFormDirty(string change)
    {
        var form = new ExerciseFormViewModel(Breathing());

        switch (change)
        {
            case "name": form.Name = "Other"; break;
            case "description": form.Description = "Other"; break;
            case "repeat": form.IsRepeating = false; break;
            case "text": form.Instructions[1].Text = "Hold"; break;
            case "duration": form.Instructions[1].DurationSeconds = 5; break;
            case "phase": SetPhase(form.Instructions[1], BreathingPhaseType.HoldOut); break;
            case "add": form.AddInstructionCommand.Execute(null); break;
            case "remove": form.RemoveInstructionCommand.Execute(form.Instructions[1]); break;
            case "move": form.MoveInstructionUpCommand.Execute(form.Instructions[1]); break;
        }

        form.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void RevertingTheChanges_MakesTheFormCleanAgain()
    {
        var form = new ExerciseFormViewModel(Breathing());

        form.Name = "Other";
        form.Instructions[0].DurationSeconds = 9;
        form.MoveInstructionDownCommand.Execute(form.Instructions[0]);
        form.IsDirty.Should().BeTrue();

        form.MoveInstructionUpCommand.Execute(form.Instructions[1]);
        form.Instructions[0].DurationSeconds = 4;
        form.Name = "Box";

        form.IsDirty.Should().BeFalse();
    }

    [Fact]
    public void DefinitionChanged_IsRaisedWhenTheWayTheExerciseRunsChanges()
    {
        var form = new ExerciseFormViewModel(Breathing());
        var raised = 0;
        form.DefinitionChanged += (_, _) => raised++;

        form.Name = "Other";
        form.Description = "Other";
        raised.Should().Be(0);

        form.IsRepeating = false;
        form.Instructions[0].Text = "Slowly in";
        form.Instructions[0].DurationSeconds = 5;
        form.AddInstructionCommand.Execute(null);

        raised.Should().Be(4);
    }

    [Theory]
    [InlineData(nameof(WellnessSessionType.Breathing))]
    [InlineData(nameof(WellnessSessionType.Meditation))]
    public void NewForm_StartsACleanRepeatingDraftWithOneInstruction(string typeName)
    {
        var type = Enum.Parse<WellnessSessionType>(typeName);
        var (expectedDuration, expectedPhase) = type == WellnessSessionType.Breathing
            ? (4, BreathingPhaseType.Inhale)
            : (60, (BreathingPhaseType?)null);

        var form = new ExerciseFormViewModel(type);

        form.Id.Should().BeNull();
        form.IsCreatingNew.Should().BeTrue();
        form.Type.Should().Be(type);
        form.Name.Should().BeEmpty();
        form.Description.Should().BeEmpty();
        form.IsRepeating.Should().BeTrue();
        form.Instructions.Select(i => (i.Number, i.Text, i.DurationSeconds, i.Phase)).Should().Equal(
            (1, "", expectedDuration, expectedPhase));
        form.IsDirty.Should().BeFalse();
        form.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Type_OfANewExercise_SwitchingToMeditationDropsThePhases()
    {
        var form = new ExerciseFormViewModel(WellnessSessionType.Breathing);
        form.AddInstructionCommand.Execute(null);
        form.Instructions[1].Text = "Hold";

        form.Type = WellnessSessionType.Meditation;

        form.Instructions.Select(i => (i.Number, i.Text, i.DurationSeconds, i.Phase, i.SupportsPhases)).Should().Equal(
            (1, "", 4, (BreathingPhaseType?)null, false),
            (2, "Hold", 4, (BreathingPhaseType?)null, false));
        form.IsDirty.Should().BeTrue();

        // meditation instructions need their text now
        form.Instructions[0].HasErrors.Should().BeTrue();

        form.Type = WellnessSessionType.Breathing;

        form.Instructions.Should().AllSatisfy(i =>
        {
            i.SupportsPhases.Should().BeTrue();
            i.Phase.Should().BeNull();
        });
    }

    [Fact]
    public void Type_OfAnExistingExercise_CannotChange()
    {
        var form = new ExerciseFormViewModel(Breathing());

        form.Type = WellnessSessionType.Meditation;

        form.Type.Should().Be(WellnessSessionType.Breathing);
        form.Instructions.Select(i => i.Phase).Should().Equal(
            BreathingPhaseType.Inhale, BreathingPhaseType.HoldIn, BreathingPhaseType.Exhale);
        form.IsDirty.Should().BeFalse();
    }

    [Theory]
    [InlineData(nameof(WellnessSessionType.Breathing))]
    [InlineData(nameof(WellnessSessionType.Meditation))]
    public void ToRequest_OfANewExercise_CreatesTheChosenTypeWithoutId(string typeName)
    {
        var type = Enum.Parse<WellnessSessionType>(typeName);
        var form = new ExerciseFormViewModel(WellnessSessionType.Breathing)
        {
            Type = type,
            Name = "Evening",
            Description = "Wind down"
        };
        form.Instructions[0].Text = "Settle";
        form.Instructions[0].DurationSeconds = 30;

        var request = form.ToRequest();

        var expectedPhase = type == WellnessSessionType.Breathing ? BreathingPhaseType.Inhale : (BreathingPhaseType?)null;
        request.Should().BeEquivalentTo(new UpsertWellnessExerciseRequest(null, type, "Evening", "Wind down", true,
            [new UpsertExerciseInstructionRequest("Settle", 30, expectedPhase)]), options => options.WithStrictOrdering());
        form.IsValid.Should().BeTrue();
    }
}
