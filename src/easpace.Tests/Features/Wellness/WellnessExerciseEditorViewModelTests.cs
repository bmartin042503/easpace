// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Repositories;
using easpace.Desktop.Features.Wellness.ViewModels.Exercises;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Wellness;

public class WellnessExerciseEditorViewModelTests
{
    [Fact]
    public void Constructor_AllowsSavingAValidExercise()
    {
        var editor = CreateEditor();

        editor.HasErrors.Should().BeFalse();
        editor.HasInstructions.Should().BeTrue();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Name_DisablesSavingWhileInvalid()
    {
        var editor = CreateEditor();

        editor.Name = "ab";

        editor.HasErrors.Should().BeTrue();
        editor.GetErrors(nameof(editor.Name)).Cast<ValidationIssue>()
            .Should().Equal(new ValidationIssue("Name", "Validation.Name.MinLength"));
        editor.SaveCommand.CanExecute(null).Should().BeFalse();

        editor.Name = "Box Breathing";

        editor.HasErrors.Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Instruction_DisablesSavingWhileInvalid()
    {
        var editor = CreateEditor();
        var instruction = editor.Instructions[0];

        instruction.Text = string.Empty;

        instruction.GetErrors(nameof(instruction.Text)).Cast<ValidationIssue>()
            .Should().Equal(new ValidationIssue("Text", "Validation.Instruction.ContentRequired"));
        editor.SaveCommand.CanExecute(null).Should().BeFalse();

        instruction.BreathingPhase = BreathingPhase.Inhale;

        instruction.HasErrors.Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RemoveInstruction_DisablesSavingWhenNoInstructionIsLeft()
    {
        var editor = CreateEditor();

        editor.RemoveInstructionCommand.Execute(editor.Instructions[0]);

        editor.HasInstructions.Should().BeFalse();
        editor.GetErrors(nameof(editor.Instructions)).Cast<ValidationIssue>()
            .Should().Equal(new ValidationIssue("Instructions", "Validation.Instructions.Required"));
        editor.SaveCommand.CanExecute(null).Should().BeFalse();

        // a new instruction has no content yet, so saving stays disabled until it gets one
        editor.AddInstructionCommand.Execute(null);

        editor.HasErrors.Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeFalse();

        editor.Instructions[0].Text = "Notice your breath";

        editor.HasInstructions.Should().BeTrue();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    private static WellnessExerciseEditorViewModel CreateEditor()
    {
        var exercise = new WellnessExercise
        {
            Name = "Box Breathing",
            DefaultCycleCount = 4,
            Instructions = [new WellnessExerciseInstruction { Text = "Breathe in", DurationSeconds = 4 }]
        };

        return new WellnessExerciseEditorViewModel(
            exercise,
            new Mock<IWellnessExerciseRepository>().Object,
            new Mock<IDialogService>().Object,
            new Mock<ILogger<WellnessExerciseEditorViewModel>>().Object);
    }
}
