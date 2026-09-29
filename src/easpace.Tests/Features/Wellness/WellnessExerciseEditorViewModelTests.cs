// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

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

        var editor = new WellnessExerciseEditorViewModel(
            _exerciseServiceMock.Object,
            _dialogServiceMock.Object,
            new Mock<ILogger<WellnessExerciseEditorViewModel>>().Object,
            selectedExerciseId);

        await editor.InitializeAsync();
        return editor;
    }

    private void AnswerDiscardDialog(bool confirm) =>
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
            dialog.Message == LocalizationService.GetString("Wellness.Editor.Error.SaveFailed"))), Times.Once);
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
        AnswerDiscardDialog(confirm: false);
        var closed = false;
        editor.Closed += (_, _) => closed = true;

        await editor.NavigateBackCommand.ExecuteAsync(null);

        closed.Should().BeFalse();
        editor.Form!.Name.Should().Be("Square");
        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.Is<ConfirmDialogViewModel>(dialog =>
            dialog.IsDestructive && dialog.Title == LocalizationService.GetString("Wellness.Editor.DiscardDialog.Title"))), Times.Once);
    }

    [Fact]
    public async Task NavigateBack_WithChanges_ClosesWhenTheUserConfirms()
    {
        var editor = await CreateInitializedEditorAsync(_box.Id);
        editor.Form!.Name = "Square";
        AnswerDiscardDialog(confirm: true);
        var closed = false;
        editor.Closed += (_, _) => closed = true;

        await editor.NavigateBackCommand.ExecuteAsync(null);

        closed.Should().BeTrue();
        _exerciseServiceMock.Verify(s => s.UpdateExerciseAsync(
            It.IsAny<Guid>(), It.IsAny<UpsertWellnessExerciseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
