// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Headless.XUnit;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Repositories;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Features.Wellness.ViewModels.Exercises;
using easpace.Desktop.Services.Presentation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Wellness;

public class WellnessExercisesViewModelTests
{
    [AvaloniaFact]
    public async Task Initialize_LoadsTheFirstExerciseIntoThePreview()
    {
        using var exercisesVm = CreateExercisesViewModel(
            CreateExercise("First", 2, 4),
            CreateExercise("Second", 1, 6));

        await exercisesVm.InitializeCommand.ExecuteAsync(null);

        exercisesVm.SelectedExerciseViewModel!.Name.Should().Be("First");
        exercisesVm.PreviewViewModel.CurrentInstruction!.Text.Should().Be("First");
        exercisesVm.PreviewViewModel.TotalCycles.Should().Be(2);
        exercisesVm.PreviewViewModel.State.Should().Be(WellnessExercisePlaybackState.Paused);

        exercisesVm.SelectedExerciseViewModel = exercisesVm.WellnessExercises[1];

        exercisesVm.PreviewViewModel.CurrentInstruction!.Text.Should().Be("Second");
        exercisesVm.PreviewViewModel.TotalCycles.Should().Be(1);
        exercisesVm.PreviewViewModel.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(6));
    }

    [AvaloniaFact]
    public async Task Selection_ClearsThePreviewWhenTheExerciseIsNotPlayable()
    {
        using var exercisesVm = CreateExercisesViewModel(
            CreateExercise("Playable", 1, 4),
            new WellnessExercise { Name = "Empty", DefaultCycleCount = 1 });

        await exercisesVm.InitializeCommand.ExecuteAsync(null);
        exercisesVm.SelectedExerciseViewModel = exercisesVm.WellnessExercises[1];

        exercisesVm.PreviewViewModel.CurrentInstruction.Should().BeNull();
        exercisesVm.PreviewViewModel.PlayCommand.CanExecute(null).Should().BeFalse();

        exercisesVm.SelectedExerciseViewModel = null;

        exercisesVm.PreviewViewModel.CurrentInstruction.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task Dispose_IgnoresSelectionsAfterNavigatingAway()
    {
        var exercisesVm = CreateExercisesViewModel(CreateExercise("First", 1, 4));
        await exercisesVm.InitializeCommand.ExecuteAsync(null);
        var selectedExercise = exercisesVm.SelectedExerciseViewModel;

        exercisesVm.Dispose();
        var select = () => exercisesVm.SelectedExerciseViewModel = null;

        select.Should().NotThrow();
        exercisesVm.ContentViewModel.Should().BeSameAs(selectedExercise);
        exercisesVm.PreviewViewModel.CurrentInstruction.Should().BeNull();
        exercisesVm.PreviewViewModel.PlayCommand.CanExecute(null).Should().BeFalse();
    }

    private static WellnessExercisesViewModel CreateExercisesViewModel(params WellnessExercise[] exercises)
    {
        var mockRepository = new Mock<IWellnessExerciseRepository>();
        mockRepository.Setup(r => r.GetWellnessExercisesAsync()).ReturnsAsync(exercises);

        return new WellnessExercisesViewModel(
            mockRepository.Object,
            new Mock<IDialogService>().Object,
            new WellnessExercisePreviewViewModel(new WellnessExercisePlayer(), TimeProvider.System),
            new Mock<ILogger<WellnessExercisesViewModel>>().Object);
    }

    private static WellnessExercise CreateExercise(string text, int cycleCount, int durationSeconds)
    {
        return new WellnessExercise
        {
            Name = text,
            DefaultCycleCount = cycleCount,
            Instructions = [new WellnessExerciseInstruction { Text = text, DurationSeconds = durationSeconds }]
        };
    }
}
