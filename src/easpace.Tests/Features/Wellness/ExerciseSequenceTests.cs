// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Core;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness;

public class ExerciseSequenceTests
{
    private static readonly ExerciseStep[] BoxSteps =
    [
        new("In", 4, BreathingPhaseType.Inhale),
        new("Hold", 4, BreathingPhaseType.HoldIn),
        new("Out", 4, BreathingPhaseType.Exhale),
        new("Hold", 4, BreathingPhaseType.HoldOut)
    ];

    private static readonly ExerciseStep[] ShortSteps =
    [
        new("First", 2, null),
        new("Second", 3, null)
    ];

    private static void TickTimes(ExerciseSequence sequence, int seconds)
    {
        for (var i = 0; i < seconds; i++)
        {
            sequence.Tick();
        }
    }

    [Fact]
    public void NewSequence_StartsAtFirstStepOfFirstCycle()
    {
        var sequence = new ExerciseSequence(BoxSteps, 3);

        sequence.StepIndex.Should().Be(0);
        sequence.CycleIndex.Should().Be(0);
        sequence.StepElapsedSeconds.Should().Be(0);
        sequence.StepRemainingSeconds.Should().Be(4);
        sequence.CurrentStep.Should().Be(BoxSteps[0]);
        sequence.ElapsedSeconds.Should().Be(0);
        sequence.CompletedCycles.Should().Be(0);
        sequence.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Durations_AreDerivedFromStepsAndCycles()
    {
        var sequence = new ExerciseSequence(BoxSteps, 3);

        sequence.CycleSeconds.Should().Be(16);
        sequence.TotalSeconds.Should().Be(48);
    }

    [Fact]
    public void TotalSeconds_WhenLooping_IsNull()
    {
        new ExerciseSequence(BoxSteps, null).TotalSeconds.Should().BeNull();
    }

    [Fact]
    public void Tick_WithinStep_OnlyAdvancesElapsedTime()
    {
        var sequence = new ExerciseSequence(ShortSteps, 1);

        var changed = sequence.Tick();

        changed.Should().BeFalse();
        sequence.StepIndex.Should().Be(0);
        sequence.StepElapsedSeconds.Should().Be(1);
        sequence.StepRemainingSeconds.Should().Be(1);
    }

    [Fact]
    public void Tick_AtStepBoundary_MovesToNextStep()
    {
        var sequence = new ExerciseSequence(ShortSteps, 1);
        sequence.Tick();

        var changed = sequence.Tick();

        changed.Should().BeTrue();
        sequence.StepIndex.Should().Be(1);
        sequence.StepElapsedSeconds.Should().Be(0);
        sequence.StepRemainingSeconds.Should().Be(3);
        sequence.ElapsedSeconds.Should().Be(2);
    }

    [Fact]
    public void Tick_AfterLastStepOfCycle_WrapsIntoNextCycle()
    {
        var sequence = new ExerciseSequence(ShortSteps, 2);

        TickTimes(sequence, 5);

        sequence.StepIndex.Should().Be(0);
        sequence.CycleIndex.Should().Be(1);
        sequence.CompletedCycles.Should().Be(1);
        sequence.ElapsedSeconds.Should().Be(5);
        sequence.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Tick_CompletesExactlyAfterCyclesTimesCycleSeconds()
    {
        var sequence = new ExerciseSequence(BoxSteps, 3);

        TickTimes(sequence, 47);
        sequence.IsCompleted.Should().BeFalse();

        var changed = sequence.Tick();

        changed.Should().BeTrue();
        sequence.IsCompleted.Should().BeTrue();
        sequence.CompletedCycles.Should().Be(3);
        sequence.ElapsedSeconds.Should().Be(48);
        sequence.StepRemainingSeconds.Should().Be(0);
        sequence.StepIndex.Should().Be(3);
        sequence.CycleIndex.Should().Be(2);
    }

    [Fact]
    public void Tick_SingleCycle_CompletesAfterOnePass()
    {
        var sequence = new ExerciseSequence(ShortSteps, 1);

        TickTimes(sequence, 5);

        sequence.IsCompleted.Should().BeTrue();
        sequence.CompletedCycles.Should().Be(1);
    }

    [Fact]
    public void Tick_WhenCompleted_DoesNothing()
    {
        var sequence = new ExerciseSequence(ShortSteps, 1);
        TickTimes(sequence, 5);

        var changed = sequence.Tick();

        changed.Should().BeFalse();
        sequence.ElapsedSeconds.Should().Be(5);
        sequence.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Tick_WhenLooping_NeverCompletes()
    {
        var sequence = new ExerciseSequence(ShortSteps, null);

        TickTimes(sequence, 5 * 100 + 2);

        sequence.IsCompleted.Should().BeFalse();
        sequence.CycleIndex.Should().Be(100);
        sequence.CompletedCycles.Should().Be(100);
        sequence.StepIndex.Should().Be(1);
        sequence.StepElapsedSeconds.Should().Be(0);
    }

    [Fact]
    public void MoveNext_JumpsToStartOfNextStep()
    {
        var sequence = new ExerciseSequence(BoxSteps, 2);
        sequence.Tick();

        var moved = sequence.MoveNext();

        moved.Should().BeTrue();
        sequence.StepIndex.Should().Be(1);
        sequence.StepElapsedSeconds.Should().Be(0);
        sequence.ElapsedSeconds.Should().Be(4);
    }

    [Fact]
    public void MoveNext_FromLastStep_WrapsIntoNextCycle()
    {
        var sequence = new ExerciseSequence(ShortSteps, 2);
        sequence.MoveNext();

        sequence.MoveNext();

        sequence.StepIndex.Should().Be(0);
        sequence.CycleIndex.Should().Be(1);
        sequence.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void MoveNext_FromFinalStep_CompletesSequence()
    {
        var sequence = new ExerciseSequence(ShortSteps, 2);
        sequence.MoveNext();
        sequence.MoveNext();
        sequence.MoveNext();

        var moved = sequence.MoveNext();

        moved.Should().BeTrue();
        sequence.IsCompleted.Should().BeTrue();
        sequence.StepIndex.Should().Be(1);
        sequence.CycleIndex.Should().Be(1);
        sequence.StepRemainingSeconds.Should().Be(0);
        sequence.ElapsedSeconds.Should().Be(10);
    }

    [Fact]
    public void MoveNext_WhenCompleted_ReturnsFalse()
    {
        var sequence = new ExerciseSequence(ShortSteps, 1);
        sequence.MoveNext();
        sequence.MoveNext();

        sequence.MoveNext().Should().BeFalse();
        sequence.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void MoveNext_WhenLooping_KeepsWrapping()
    {
        var sequence = new ExerciseSequence(ShortSteps, null);

        for (var i = 0; i < 7; i++)
        {
            sequence.MoveNext().Should().BeTrue();
        }

        sequence.CycleIndex.Should().Be(3);
        sequence.StepIndex.Should().Be(1);
        sequence.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void MovePrevious_JumpsToStartOfPreviousStep()
    {
        var sequence = new ExerciseSequence(BoxSteps, 1);
        sequence.MoveNext();
        sequence.MoveNext();
        sequence.Tick();

        var moved = sequence.MovePrevious();

        moved.Should().BeTrue();
        sequence.StepIndex.Should().Be(1);
        sequence.StepElapsedSeconds.Should().Be(0);
    }

    [Fact]
    public void MovePrevious_FromFirstStepOfLaterCycle_WrapsToLastStepOfPreviousCycle()
    {
        var sequence = new ExerciseSequence(ShortSteps, 3);
        sequence.MoveNext();
        sequence.MoveNext();

        var moved = sequence.MovePrevious();

        moved.Should().BeTrue();
        sequence.CycleIndex.Should().Be(0);
        sequence.StepIndex.Should().Be(1);
        sequence.StepElapsedSeconds.Should().Be(0);
    }

    [Fact]
    public void MovePrevious_AtVeryStart_ReturnsFalseAndKeepsPosition()
    {
        var sequence = new ExerciseSequence(ShortSteps, 2);
        sequence.Tick();

        var moved = sequence.MovePrevious();

        moved.Should().BeFalse();
        sequence.StepIndex.Should().Be(0);
        sequence.CycleIndex.Should().Be(0);
        sequence.StepElapsedSeconds.Should().Be(1);
    }

    [Fact]
    public void MovePrevious_WhenCompleted_ReturnsToStartOfFinalStep()
    {
        var sequence = new ExerciseSequence(ShortSteps, 2);
        TickTimes(sequence, 10);

        var moved = sequence.MovePrevious();

        moved.Should().BeTrue();
        sequence.IsCompleted.Should().BeFalse();
        sequence.CycleIndex.Should().Be(1);
        sequence.StepIndex.Should().Be(1);
        sequence.StepElapsedSeconds.Should().Be(0);
        sequence.CompletedCycles.Should().Be(1);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        var sequence = new ExerciseSequence(ShortSteps, 2);
        TickTimes(sequence, 10);

        sequence.Reset();

        sequence.IsCompleted.Should().BeFalse();
        sequence.StepIndex.Should().Be(0);
        sequence.CycleIndex.Should().Be(0);
        sequence.StepElapsedSeconds.Should().Be(0);
        sequence.ElapsedSeconds.Should().Be(0);
    }

    [Fact]
    public void Constructor_CopiesSteps()
    {
        var steps = new List<ExerciseStep>(ShortSteps);
        var sequence = new ExerciseSequence(steps, 1);

        steps.Clear();

        sequence.Steps.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_WithNullSteps_Throws()
    {
        var act = () => new ExerciseSequence(null!, 1);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptySteps_Throws()
    {
        var act = () => new ExerciseSequence([], 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Constructor_WithNonPositiveStepDuration_Throws(int duration)
    {
        var act = () => new ExerciseSequence([new ExerciseStep("First", 2, null), new ExerciseStep("Bad", duration, null)], 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithCyclesBelowOne_Throws(int cycles)
    {
        var act = () => new ExerciseSequence(ShortSteps, cycles);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(nameof(BreathingPhaseType.Inhale), "Wellness.Instruction.BreatheIn")]
    [InlineData(nameof(BreathingPhaseType.HoldIn), "Wellness.Instruction.Hold")]
    [InlineData(nameof(BreathingPhaseType.Exhale), "Wellness.Instruction.BreatheOut")]
    [InlineData(nameof(BreathingPhaseType.HoldOut), "Wellness.Instruction.Hold")]
    public void GetDefaultText_ReturnsLocalizedPhaseText(string phaseName, string key)
    {
        var text = ExerciseStep.GetDefaultText(Enum.Parse<BreathingPhaseType>(phaseName));

        text.Should().Be(LocalizationService.GetString(key));
        text.Should().NotStartWith("[");
    }

    [Fact]
    public void GetDefaultText_WithoutPhase_ReturnsEmpty()
    {
        ExerciseStep.GetDefaultText(null).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTextAndPhase_FallsBackToPhaseText(string? text)
    {
        var step = ExerciseStep.Create(text, 4, BreathingPhaseType.Exhale);

        step.Text.Should().Be(LocalizationService.GetString("Wellness.Instruction.BreatheOut"));
        step.DurationSeconds.Should().Be(4);
        step.Phase.Should().Be(BreathingPhaseType.Exhale);
    }

    [Fact]
    public void Create_WithText_KeepsTrimmedCustomText()
    {
        var step = ExerciseStep.Create("  Slowly fill your lungs  ", 5, BreathingPhaseType.Inhale);

        step.Text.Should().Be("Slowly fill your lungs");
    }

    [Fact]
    public void Create_WithTextAndNoPhase_CreatesPlainStep()
    {
        var step = ExerciseStep.Create("Notice your breath", 60, null);

        step.Text.Should().Be("Notice your breath");
        step.Phase.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithBlankTextAndNoPhase_Throws(string? text)
    {
        var act = () => ExerciseStep.Create(text, 60, null);

        act.Should().Throw<ArgumentException>();
    }
}
