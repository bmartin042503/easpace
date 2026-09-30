// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Features.Wellness.ViewModels;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels.Dialogs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Wellness;

public class WellnessExerciseEditorViewModelTests
{
    private readonly Mock<IWellnessExerciseService> _exerciseServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();
    private readonly Mock<IToastMessageService> _toastServiceMock = new();

    private readonly BreathingExercise _box = new()
    {
        Name = "Box",
        IsRepeating = true,
        Instructions =
        [
            new ExerciseInstruction { Order = 1, DurationSeconds = 4, Phase = BreathingPhaseType.Inhale },
            new ExerciseInstruction { Order = 2, DurationSeconds = 4, Phase = BreathingPhaseType.Exhale }
        ]
    };

    private readonly MeditationExercise _calm = new()
    {
        Name = "Calm",
        IsRepeating = true,
        Instructions = [new ExerciseInstruction { Order = 1, DurationSeconds = 60, Text = "Sit" }]
    };

    private void SetUpExercises(params WellnessExercise[] exercises) =>
        _exerciseServiceMock
            .Setup(s => s.GetExercisesAsync(It.IsAny<WellnessSessionType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercises);

    private async Task<WellnessExerciseEditorViewModel> CreateInitializedEditorAsync(Guid? selectedExerciseId)
    {
        SetUpExercises(_box, _calm);
        return await InitializeEditorAsync(selectedExerciseId);
    }

    // the exercises have to be set up before
    private async Task<WellnessExerciseEditorViewModel> InitializeEditorAsync(Guid? selectedExerciseId)
    {
        var editor = new WellnessExerciseEditorViewModel(
            _exerciseServiceMock.Object,
            _dialogServiceMock.Object,
            _toastServiceMock.Object,
            new Mock<ILogger<WellnessExerciseEditorViewModel>>().Object,
            selectedExerciseId);

        await editor.InitializeAsync();
        return editor;
    }

    private static BreathingExercise Breathing(string name) => new()
    {
        Name = name,
        IsRepeating = true,
        Instructions = [new ExerciseInstruction { Order = 1, DurationSeconds = 4, Phase = BreathingPhaseType.Inhale }]
    };

    private void AnswerConfirmation(bool confirm) =>
        _dialogServiceMock
            .Setup(d => d.ShowDialogAsync(It.IsAny<ConfirmDialogViewModel>()))
            .Callback<ConfirmDialogViewModel>(dialog =>
            {
                if (confirm) dialog.ConfirmCommand.Execute(null);
                else dialog.CancelCommand.Execute(null);
            })
            .Returns(Task.CompletedTask);

    [Fact]
    public async Task Initialize_SelectsTheRequestedExerciseWithACleanForm()
    {
        var editor = await CreateInitializedEditorAsync(_calm.Id);

        editor.SelectedExercise!.Id.Should().Be(_calm.Id);
        editor.Form!.Id.Should().Be(_calm.Id);
        editor.Form.Name.Should().Be("Calm");
        editor.Form.IsDirty.Should().BeFalse();
        editor.CanChangeSelection.Should().BeTrue();
        editor.SaveCommand.CanExecute(null).Should().BeFalse();
        editor.DiscardCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task EditingTheForm_LocksTheSelectionAndEnablesSaveAndDiscard()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);

        editor.Form!.Name = "Square";

        editor.CanChangeSelection.Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
        editor.DiscardCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task InvalidForm_CannotBeSaved()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);

        editor.Form!.Name = "";

        editor.SaveCommand.CanExecute(null).Should().BeFalse();
        await editor.SaveCommand.ExecuteAsync(null);
        _exerciseServiceMock.Verify(s => s.UpdateExerciseAsync(
            It.IsAny<Guid>(), It.IsAny<UpsertWellnessExerciseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Save_UpdatesTheExerciseAndReloadsWithACleanForm()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Form!.Name = "Square";
        editor.Form.Instructions[1].DurationSeconds = 6;

        UpsertWellnessExerciseRequest? savedRequest = null;
        _exerciseServiceMock
            .Setup(s => s.UpdateExerciseAsync(_box.Id, It.IsAny<UpsertWellnessExerciseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, UpsertWellnessExerciseRequest, CancellationToken>((_, request, _) =>
            {
                savedRequest = request;
                _box.Name = request.Name;
                _box.Instructions.Single(i => i.Order == 2).DurationSeconds = 6;
            })
            .ReturnsAsync(_box);

        await editor.SaveCommand.ExecuteAsync(null);

        savedRequest.Should().NotBeNull();
        savedRequest!.Name.Should().Be("Square");
        savedRequest.Instructions.Select(i => i.DurationSeconds).Should().Equal(4, 6);
        editor.SelectedExercise!.Id.Should().Be(_box.Id);
        editor.SelectedExercise.Name.Should().Be("Square");
        editor.Form!.Name.Should().Be("Square");
        editor.Form.IsDirty.Should().BeFalse();
        editor.CanChangeSelection.Should().BeTrue();
    }

    [Fact]
    public async Task Save_WhenTheServiceFails_ShowsAnErrorAndKeepsTheChanges()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Form!.Name = "Square";
        _exerciseServiceMock
            .Setup(s => s.UpdateExerciseAsync(_box.Id, It.IsAny<UpsertWellnessExerciseRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database is locked"));

        await editor.SaveCommand.ExecuteAsync(null);

        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.Is<ErrorDialogViewModel>(dialog =>
            dialog.Message == LocalizationService.GetString("Wellness.Error.ExerciseSaveFailed"))), Times.Once);
        editor.Form!.Name.Should().Be("Square");
        editor.Form.IsDirty.Should().BeTrue();
    }

    [Fact]
    public async Task Discard_RevertsTheFormToTheStoredExercise()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Form!.Name = "Square";
        editor.Form.RemoveInstructionCommand.Execute(editor.Form.Instructions[0]);

        editor.DiscardCommand.Execute(null);

        editor.Form!.Name.Should().Be("Box");
        editor.Form.Instructions.Select(i => i.Phase).Should().Equal(BreathingPhaseType.Inhale, BreathingPhaseType.Exhale);
        editor.Form.IsDirty.Should().BeFalse();
        editor.CanChangeSelection.Should().BeTrue();
    }

    [Fact]
    public async Task NavigateBack_WithoutChanges_ClosesWithoutAsking()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        var closed = false;
        editor.Closed += (_, _) => closed = true;

        await editor.NavigateBackCommand.ExecuteAsync(null);

        closed.Should().BeTrue();
        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.IsAny<ConfirmDialogViewModel>()), Times.Never);
    }

    [Fact]
    public async Task NavigateBack_WithChanges_StaysWhenTheUserCancels()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Form!.Name = "Square";
        AnswerConfirmation(confirm: false);
        var closed = false;
        editor.Closed += (_, _) => closed = true;

        await editor.NavigateBackCommand.ExecuteAsync(null);

        closed.Should().BeFalse();
        editor.Form!.Name.Should().Be("Square");
        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.Is<ConfirmDialogViewModel>(dialog =>
            dialog.IsDestructive && dialog.Title == LocalizationService.GetString("Wellness.DiscardChangesDialog.Title"))), Times.Once);
    }

    [Fact]
    public async Task NavigateBack_WithChanges_ClosesWhenTheUserConfirms()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Form!.Name = "Square";
        AnswerConfirmation(confirm: true);
        var closed = false;
        editor.Closed += (_, _) => closed = true;

        await editor.NavigateBackCommand.ExecuteAsync(null);

        closed.Should().BeTrue();
        _exerciseServiceMock.Verify(s => s.UpdateExerciseAsync(
            It.IsAny<Guid>(), It.IsAny<UpsertWellnessExerciseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewExercise_StartsACleanDraftOfTheSelectedType()
    {
        var editor = await CreateInitializedEditorAsync(_calm.Id);

        editor.CreateNewExerciseCommand.Execute(null);

        editor.SelectedExercise.Should().BeNull();
        editor.Form!.IsCreatingNew.Should().BeTrue();
        editor.Form.Id.Should().BeNull();
        editor.Form.Type.Should().Be(WellnessSessionType.Meditation);
        editor.Form.Name.Should().BeEmpty();
        editor.Form.IsDirty.Should().BeFalse();
        editor.ShowNoExercises.Should().BeFalse();
    }

    [Fact]
    public async Task CreateNewExercise_WithoutExercises_StartsABreathingDraft()
    {
        SetUpExercises();
        var editor = await InitializeEditorAsync(null);
        editor.ShowNoExercises.Should().BeTrue();

        editor.CreateNewExerciseCommand.Execute(null);

        editor.Form!.Type.Should().Be(WellnessSessionType.Breathing);
        editor.ShowNoExercises.Should().BeFalse();
    }

    [Fact]
    public async Task CreateNewExercise_LocksTheHeaderUntilTheDraftIsDiscarded()
    {
        var editor = await CreateInitializedEditorAsync(_calm.Id);

        editor.CreateNewExerciseCommand.Execute(null);

        editor.CanChangeSelection.Should().BeFalse();
        editor.CreateNewExerciseCommand.CanExecute(null).Should().BeFalse();
        editor.DeleteExerciseCommand.CanExecute(null).Should().BeFalse();
        editor.RestoreDefaultsCommand.CanExecute(null).Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeFalse();
        editor.DiscardCommand.CanExecute(null).Should().BeTrue();

        editor.DiscardCommand.Execute(null);

        editor.SelectedExercise!.Id.Should().Be(_calm.Id);
        editor.Form!.Id.Should().Be(_calm.Id);
        editor.CanChangeSelection.Should().BeTrue();
        editor.CreateNewExerciseCommand.CanExecute(null).Should().BeTrue();
        editor.DeleteExerciseCommand.CanExecute(null).Should().BeTrue();
        editor.RestoreDefaultsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task DiscardingADraft_WithoutExercises_ClearsTheForm()
    {
        SetUpExercises();
        var editor = await InitializeEditorAsync(null);
        editor.CreateNewExerciseCommand.Execute(null);

        editor.DiscardCommand.Execute(null);

        editor.Form.Should().BeNull();
        editor.ShowNoExercises.Should().BeTrue();
    }

    [Theory]
    [InlineData(nameof(WellnessSessionType.Breathing))]
    [InlineData(nameof(WellnessSessionType.Meditation))]
    public async Task Save_OfADraft_CreatesTheExerciseAndSelectsIt(string typeName)
    {
        var type = Enum.Parse<WellnessSessionType>(typeName);
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.CreateNewExerciseCommand.Execute(null);
        editor.Form!.Type = type;
        editor.Form.Name = "Aaa";
        editor.Form.Instructions[0].Text = "Relax";

        UpsertWellnessExerciseRequest? createRequest = null;
        _exerciseServiceMock
            .Setup(s => s.CreateExerciseAsync(It.IsAny<UpsertWellnessExerciseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UpsertWellnessExerciseRequest, CancellationToken>((request, _) => createRequest = request)
            .ReturnsAsync((UpsertWellnessExerciseRequest request, CancellationToken _) =>
            {
                WellnessExercise created = request.Type == WellnessSessionType.Breathing ? new BreathingExercise() : new MeditationExercise();
                created.Name = request.Name;
                created.IsRepeating = request.IsRepeating;
                created.Instructions = request.Instructions
                    .Select((i, index) => new ExerciseInstruction { Order = index + 1, Text = i.Text, DurationSeconds = i.DurationSeconds, Phase = i.Phase })
                    .ToList();
                return created;
            });

        await editor.SaveCommand.ExecuteAsync(null);

        createRequest.Should().NotBeNull();
        createRequest!.Id.Should().BeNull();
        createRequest.Type.Should().Be(type);
        createRequest.Name.Should().Be("Aaa");
        _exerciseServiceMock.Verify(s => s.UpdateExerciseAsync(
            It.IsAny<Guid>(), It.IsAny<UpsertWellnessExerciseRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        // sorted by type, then by name, so "Aaa" is the first of its type
        var expectedNames = type == WellnessSessionType.Breathing ? new[] { "Aaa", "Box", "Calm" } : ["Box", "Aaa", "Calm"];
        editor.Exercises.Select(e => e.Name).Should().Equal(expectedNames);
        editor.SelectedExercise!.Name.Should().Be("Aaa");
        editor.Form!.IsCreatingNew.Should().BeFalse();
        editor.Form.Id.Should().Be(editor.SelectedExercise.Id);
        editor.Form.IsDirty.Should().BeFalse();
        editor.CanChangeSelection.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteExercise_WhenConfirmed_DeletesItAndSelectsTheNextOne()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        AnswerConfirmation(confirm: true);

        await editor.DeleteExerciseCommand.ExecuteAsync(null);

        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.Is<ConfirmDialogViewModel>(dialog =>
            dialog.IsDestructive && dialog.Title == LocalizationService.GetString("Wellness.DeleteExerciseDialog.Title"))), Times.Once);
        _exerciseServiceMock.Verify(s => s.DeleteExerciseAsync(_box.Id, It.IsAny<CancellationToken>()), Times.Once);
        editor.Exercises.Select(e => e.Id).Should().Equal(_calm.Id);
        editor.SelectedExercise!.Id.Should().Be(_calm.Id);
        editor.Form!.Id.Should().Be(_calm.Id);
    }

    [Fact]
    public async Task DeleteExercise_OfTheLastOne_SelectsThePreviousOne()
    {
        var editor = await CreateInitializedEditorAsync(_calm.Id);
        AnswerConfirmation(confirm: true);

        await editor.DeleteExerciseCommand.ExecuteAsync(null);

        editor.SelectedExercise!.Id.Should().Be(_box.Id);
    }

    [Fact]
    public async Task DeleteExercise_OfTheOnlyOne_ShowsTheEmptyState()
    {
        SetUpExercises(_box);
        var editor = await InitializeEditorAsync(_box.Id);
        AnswerConfirmation(confirm: true);

        await editor.DeleteExerciseCommand.ExecuteAsync(null);

        editor.Exercises.Should().BeEmpty();
        editor.SelectedExercise.Should().BeNull();
        editor.Form.Should().BeNull();
        editor.ShowNoExercises.Should().BeTrue();
        editor.DeleteExerciseCommand.CanExecute(null).Should().BeFalse();
        editor.RestoreDefaultsCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteExercise_WhenCancelled_KeepsTheExercise()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        AnswerConfirmation(confirm: false);

        await editor.DeleteExerciseCommand.ExecuteAsync(null);

        _exerciseServiceMock.Verify(s => s.DeleteExerciseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        editor.Exercises.Should().HaveCount(2);
        editor.SelectedExercise!.Id.Should().Be(_box.Id);
    }

    [Fact]
    public async Task DeleteExercise_WhenTheServiceFails_ShowsAnErrorAndKeepsTheExercise()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        AnswerConfirmation(confirm: true);
        _exerciseServiceMock
            .Setup(s => s.DeleteExerciseAsync(_box.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database is locked"));

        await editor.DeleteExerciseCommand.ExecuteAsync(null);

        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.Is<ErrorDialogViewModel>(dialog =>
            dialog.Message == LocalizationService.GetString("Wellness.Error.ExerciseDeleteFailed"))), Times.Once);
        editor.Exercises.Should().HaveCount(2);
        editor.SelectedExercise!.Id.Should().Be(_box.Id);
    }

    [Fact]
    public async Task UnsavedChanges_DisableDeleteAndRestore()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);

        editor.Form!.Name = "Square";

        editor.CreateNewExerciseCommand.CanExecute(null).Should().BeFalse();
        editor.DeleteExerciseCommand.CanExecute(null).Should().BeFalse();
        editor.RestoreDefaultsCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task RestoreDefaults_WithRestoredExercises_ShowsTheCountAndSelectsTheFirstRestoredOne()
    {
        var fourSix = Breathing("4-6");
        var triangle = Breathing("Triangle");
        _exerciseServiceMock
            .SetupSequence(s => s.GetExercisesAsync(It.IsAny<WellnessSessionType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([_box, _calm])
            .ReturnsAsync([fourSix, _box, triangle, _calm]);
        _exerciseServiceMock
            .Setup(s => s.RestoreDefaultExercisesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var editor = await InitializeEditorAsync(_calm.Id);

        await editor.RestoreDefaultsCommand.ExecuteAsync(null);

        _toastServiceMock.Verify(t => t.ShowToastMessage(
            string.Format(LocalizationService.GetString("Wellness.ToastMessage.DefaultsRestored"), 2),
            ToastMessageType.Success), Times.Once);
        editor.Exercises.Select(e => e.Name).Should().Equal("4-6", "Box", "Triangle", "Calm");
        editor.SelectedExercise!.Id.Should().Be(fourSix.Id);
        editor.Form!.Id.Should().Be(fourSix.Id);
    }

    [Fact]
    public async Task RestoreDefaults_WithNothingToRestore_ShowsAnInfoAndKeepsTheSelection()
    {
        var editor = await CreateInitializedEditorAsync(_calm.Id);
        _exerciseServiceMock
            .Setup(s => s.RestoreDefaultExercisesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await editor.RestoreDefaultsCommand.ExecuteAsync(null);

        _toastServiceMock.Verify(t => t.ShowToastMessage(
            LocalizationService.GetString("Wellness.ToastMessage.NothingToRestore"), ToastMessageType.Info), Times.Once);
        _exerciseServiceMock.Verify(s => s.GetExercisesAsync(It.IsAny<WellnessSessionType?>(), It.IsAny<CancellationToken>()), Times.Once);
        editor.SelectedExercise!.Id.Should().Be(_calm.Id);
    }

    [Fact]
    public async Task RestoreDefaults_WhenTheServiceFails_ShowsAnError()
    {
        var editor = await CreateInitializedEditorAsync(_calm.Id);
        _exerciseServiceMock
            .Setup(s => s.RestoreDefaultExercisesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database is locked"));

        await editor.RestoreDefaultsCommand.ExecuteAsync(null);

        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.Is<ErrorDialogViewModel>(dialog =>
            dialog.Message == LocalizationService.GetString("Wellness.Error.DefaultsRestoreFailed"))), Times.Once);
        _toastServiceMock.Verify(t => t.ShowToastMessage(It.IsAny<string>(), It.IsAny<ToastMessageType>()), Times.Never);
    }

    [Fact]
    public async Task Initialize_LoadsThePreviewOfTheSelectedExercise()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);

        editor.Preview.HasSteps.Should().BeTrue();
        editor.Preview.Progress!.StepCount.Should().Be(2);
        editor.Preview.ShowBreathingCircle.Should().BeTrue();
        editor.Form!.Instructions.Select(i => i.IsPreviewing).Should().Equal(true, false);
    }

    [Fact]
    public async Task PreviewSteps_HighlightTheirInstruction()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);

        editor.Preview.StepForwardCommand.Execute(null);

        editor.Form!.Instructions.Select(i => i.IsPreviewing).Should().Equal(false, true);
    }

    [Fact]
    public async Task EditingTheInstructions_ReloadsThePreviewAtItsPosition()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Preview.StepForwardCommand.Execute(null);

        editor.Form!.AddInstructionCommand.Execute(null);

        editor.Preview.Progress!.StepCount.Should().Be(3);
        editor.Preview.Progress.StepIndex.Should().Be(1);

        editor.Form.MoveInstructionUpCommand.Execute(editor.Form.Instructions[1]);

        // the step index stays, so the instruction that moved into its place is highlighted
        editor.Preview.InstructionText.Should().Be(ExerciseStep.GetDefaultText(BreathingPhaseType.Inhale));
        editor.Form.Instructions.Select(i => i.IsPreviewing).Should().Equal(false, true, false);
    }

    [Fact]
    public async Task SelectingAnotherExercise_RestartsThePreviewWithItsSteps()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Preview.StepForwardCommand.Execute(null);

        editor.SelectedExercise = editor.Exercises.Single(e => e.Id == _calm.Id);

        editor.Preview.Progress!.StepIndex.Should().Be(0);
        editor.Preview.Progress.CycleIndex.Should().Be(0);
        editor.Preview.InstructionText.Should().Be("Sit");
        editor.Preview.ShowBreathingCircle.Should().BeFalse();
        editor.Preview.ShowCountdown.Should().BeTrue();
    }

    [Fact]
    public async Task Close_DisposesThePreview()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);

        editor.Close();
        editor.Form!.AddInstructionCommand.Execute(null);

        editor.Preview.HasSteps.Should().BeFalse();
        editor.Preview.IsPlaying.Should().BeFalse();
    }
}
