// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Headless.XUnit;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Repositories;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Features.Wellness.ViewModels.Exercises;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels.Dialogs;
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

    [AvaloniaFact]
    public async Task EditExercise_ShowsUnsavedChangesInThePreview()
    {
        using var exercisesVm = CreateExercisesViewModel(CreateExercise("First", 1, 4));
        await exercisesVm.InitializeCommand.ExecuteAsync(null);

        exercisesVm.EditExerciseCommand.Execute(null);
        var editor = exercisesVm.ContentViewModel.Should().BeOfType<WellnessExerciseEditorViewModel>().Subject;

        editor.Instructions[0].Text = "Edited";
        editor.DefaultCycleCount = 3;

        exercisesVm.IsEditing.Should().BeTrue();
        exercisesVm.CanSelectExercise.Should().BeFalse();
        exercisesVm.EditExerciseCommand.CanExecute(null).Should().BeFalse();
        exercisesVm.PreviewViewModel.CurrentInstruction!.Text.Should().Be("Edited");
        exercisesVm.PreviewViewModel.TotalCycles.Should().Be(3);
    }

    [AvaloniaFact]
    public async Task CancelEdit_RestoresTheReaderViewAndTheOriginalPreview()
    {
        using var exercisesVm = CreateExercisesViewModel(CreateExercise("First", 1, 4));
        await exercisesVm.InitializeCommand.ExecuteAsync(null);
        var selectedExercise = exercisesVm.SelectedExerciseViewModel;

        exercisesVm.EditExerciseCommand.Execute(null);
        var editor = (WellnessExerciseEditorViewModel)exercisesVm.ContentViewModel!;
        editor.Instructions[0].Text = "Edited";
        editor.CancelCommand.Execute(null);

        // a change after closing must no longer reach the preview
        editor.Instructions[0].Text = "Ignored";

        exercisesVm.IsEditing.Should().BeFalse();
        exercisesVm.ContentViewModel.Should().BeSameAs(selectedExercise);
        exercisesVm.PreviewViewModel.CurrentInstruction!.Text.Should().Be("First");
    }

    [AvaloniaFact]
    public async Task BreathingPhase_NoneTurnsOffTheBreathingAnimationOfThePreview()
    {
        using var exercisesVm = CreateExercisesViewModel(CreateExercise("First", 1, 4));
        await exercisesVm.InitializeCommand.ExecuteAsync(null);
        exercisesVm.EditExerciseCommand.Execute(null);
        var instruction = exercisesVm.Editor!.Instructions[0];

        instruction.BreathingPhase = BreathingPhase.Inhale;

        exercisesVm.PreviewViewModel.IsBreathing.Should().BeTrue();

        instruction.BreathingPhase = BreathingPhase.None;

        instruction.HasBreathingPhase.Should().BeFalse();
        exercisesVm.PreviewViewModel.IsBreathing.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task SaveEdit_UpdatesTheExerciseAndReturnsToTheReaderView()
    {
        var first = CreateExercise("First", 1, 4);
        var savedExercise = CreateExercise("Edited", 2, 6);
        savedExercise.Id = first.Id;

        var mockRepository = CreateRepository(first, CreateExercise("Second", 1, 4));
        mockRepository
            .Setup(r => r.UpdateWellnessExerciseAsync(first.Id, It.IsAny<UpsertWellnessExerciseRequest>()))
            .ReturnsAsync(savedExercise);
        var mockToastMessageService = new Mock<IToastMessageService>();

        using var exercisesVm =
            CreateExercisesViewModel(mockRepository, new Mock<IDialogService>(), mockToastMessageService);
        await exercisesVm.InitializeCommand.ExecuteAsync(null);
        exercisesVm.EditExerciseCommand.Execute(null);
        exercisesVm.Editor!.Name = "Edited";

        await exercisesVm.Editor.SaveCommand.ExecuteAsync(null);

        mockRepository.Verify(r => r.UpdateWellnessExerciseAsync(first.Id,
            It.Is<UpsertWellnessExerciseRequest>(request => request.Name == "Edited")), Times.Once);
        exercisesVm.WellnessExercises.Select(e => e.Name).Should().Equal("Edited", "Second");
        exercisesVm.IsEditing.Should().BeFalse();
        exercisesVm.SelectedExerciseViewModel.Should().BeSameAs(exercisesVm.WellnessExercises[0]);
        exercisesVm.ContentViewModel.Should().BeSameAs(exercisesVm.WellnessExercises[0]);
        exercisesVm.PreviewViewModel.CurrentInstruction!.Text.Should().Be("Edited");
        exercisesVm.PreviewViewModel.TotalCycles.Should().Be(2);
        mockToastMessageService.Verify(
            t => t.ShowToastMessage(It.IsAny<string>(), ToastMessageType.Success), Times.Once);
    }

    [AvaloniaFact]
    public async Task SaveEdit_KeepsTheEditorOpenWhenSavingFails()
    {
        var first = CreateExercise("First", 1, 4);
        var mockRepository = CreateRepository(first);
        mockRepository
            .Setup(r => r.UpdateWellnessExerciseAsync(first.Id, It.IsAny<UpsertWellnessExerciseRequest>()))
            .ThrowsAsync(new InvalidOperationException());
        var mockDialogService = new Mock<IDialogService>();
        var mockToastMessageService = new Mock<IToastMessageService>();

        using var exercisesVm = CreateExercisesViewModel(mockRepository, mockDialogService, mockToastMessageService);
        await exercisesVm.InitializeCommand.ExecuteAsync(null);
        exercisesVm.EditExerciseCommand.Execute(null);
        var editor = exercisesVm.Editor!;

        await editor.SaveCommand.ExecuteAsync(null);

        mockDialogService.Verify(d => d.ShowDialogAsync(It.IsAny<ErrorDialogViewModel>()), Times.Once);
        mockToastMessageService.Verify(
            t => t.ShowToastMessage(It.IsAny<string>(), It.IsAny<ToastMessageType>()), Times.Never);
        exercisesVm.ContentViewModel.Should().BeSameAs(editor);
        exercisesVm.WellnessExercises[0].Name.Should().Be("First");
    }

    private static WellnessExercisesViewModel CreateExercisesViewModel(params WellnessExercise[] exercises)
    {
        return CreateExercisesViewModel(
            CreateRepository(exercises),
            new Mock<IDialogService>(),
            new Mock<IToastMessageService>());
    }

    private static WellnessExercisesViewModel CreateExercisesViewModel(
        Mock<IWellnessExerciseRepository> mockRepository,
        Mock<IDialogService> mockDialogService,
        Mock<IToastMessageService> mockToastMessageService)
    {
        return new WellnessExercisesViewModel(
            mockRepository.Object,
            mockDialogService.Object,
            mockToastMessageService.Object,
            new WellnessExercisePreviewViewModel(new WellnessExercisePlayer(), TimeProvider.System),
            new Mock<ILogger<WellnessExercisesViewModel>>().Object,
            new Mock<ILogger<WellnessExerciseEditorViewModel>>().Object);
    }

    private static Mock<IWellnessExerciseRepository> CreateRepository(params WellnessExercise[] exercises)
    {
        var mockRepository = new Mock<IWellnessExerciseRepository>();
        mockRepository.Setup(r => r.GetWellnessExercisesAsync()).ReturnsAsync(exercises);
        return mockRepository;
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
