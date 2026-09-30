// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

/// <summary>
/// Lets the user browse and edit the wellness exercises in place of the start view. An exercise is only shown until the
/// user starts editing it or creates a new one.
/// </summary>
internal partial class WellnessExerciseEditorViewModel : ViewModelBase
{
    private readonly IWellnessExerciseService _wellnessExerciseService;
    private readonly IDialogService _dialogService;
    private readonly IToastMessageService _toastMessageService;
    private readonly ILogger<WellnessExerciseEditorViewModel> _logger;
    private readonly Guid? _initialExerciseId;

    // the exercise to return to when a new, unsaved exercise is discarded
    private WellnessExerciseViewModel? _exerciseBeforeDraft;

    // cancels work that is still running when the editor gets closed
    private readonly CancellationTokenSource _closeTokenSource = new();

    private bool _isInitializationRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    [NotifyCanExecuteChangedFor(nameof(CreateNewExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreDefaultsCommand))]
    private bool _isInitialized;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteExerciseCommand))]
    private WellnessExerciseViewModel? _selectedExercise;

    /// <summary>
    /// Gets the form of the selected exercise or of a new exercise, or <c>null</c> when there is neither.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private ExerciseFormViewModel? _form;

    /// <summary>
    /// Gets whether the form is being edited. Otherwise the exercise is only shown, and the exercises can be browsed,
    /// created, deleted and restored.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChangeSelection))]
    [NotifyCanExecuteChangedFor(nameof(EditExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DiscardCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateNewExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteExerciseCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestoreDefaultsCommand))]
    private bool _isEditing;

    /// <summary>
    /// Gets the exercises of every session type: breathing first, then meditation, each sorted by name.
    /// </summary>
    public AvaloniaList<WellnessExerciseViewModel> Exercises { get; } = [];

    /// <summary>
    /// Gets the preview that plays the exercise as the form currently defines it.
    /// </summary>
    public ExercisePreviewViewModel Preview { get; } = new();

    public bool HasExercises => Exercises.Count > 0;
    public bool ShowNoExercises => IsInitialized && !HasExercises && Form is null;

    /// <summary>
    /// Gets whether another exercise can be selected, which isn't possible while editing.
    /// </summary>
    public bool CanChangeSelection => HasExercises && !IsEditing;

    /// <summary>
    /// Occurs when the user leaves the editor.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessExerciseEditorViewModel"/> class.
    /// </summary>
    /// <param name="selectedExerciseId">The exercise to select once loaded; the first one is selected if it's missing.</param>
    public WellnessExerciseEditorViewModel(
        IWellnessExerciseService wellnessExerciseService,
        IDialogService dialogService,
        IToastMessageService toastMessageService,
        ILogger<WellnessExerciseEditorViewModel> logger,
        Guid? selectedExerciseId)
    {
        _wellnessExerciseService = wellnessExerciseService;
        _dialogService = dialogService;
        _toastMessageService = toastMessageService;
        _logger = logger;
        _initialExerciseId = selectedExerciseId;

        Exercises.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasExercises));
            OnPropertyChanged(nameof(ShowNoExercises));
            OnPropertyChanged(nameof(CanChangeSelection));
        };

        Preview.PropertyChanged += OnPreviewPropertyChanged;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        // the view runs this each time it's shown, e.g. when returning from another page
        if (IsInitialized || _isInitializationRunning) return;

        _isInitializationRunning = true;

        try
        {
            await LoadExercisesAsync(_initialExerciseId);
            IsInitialized = true;
        }
        catch (OperationCanceledException)
        {
            // the editor was closed while loading
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the wellness exercises for the editor");
            await ShowErrorAsync("Wellness.Error.LoadFailed");
        }
        finally
        {
            _isInitializationRunning = false;
        }
    }

    /// <summary>
    /// Leaves the editor. With unsaved changes, the user has to confirm that they are discarded.
    /// </summary>
    [RelayCommand]
    private async Task NavigateBack()
    {
        if (Form is { IsDirty: true } && !await ConfirmDiscardAsync()) return;

        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts editing the selected exercise.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditExercise))]
    private void EditExercise() => IsEditing = true;

    /// <summary>
    /// Starts editing a new exercise with the type of the selected one, or breathing when nothing is selected.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCreateNewExercise))]
    private void CreateNewExercise()
    {
        var type = SelectedExercise?.SessionType ?? WellnessSessionType.Breathing;

        // the selector shows no exercise while the new one is edited
        _exerciseBeforeDraft = SelectedExercise;
        SelectedExercise = null;
        Form = new ExerciseFormViewModel(type);
        IsEditing = true;
    }

    /// <summary>
    /// Saves the form and stops editing. A new exercise is added to the list and selected; an existing one is saved and
    /// the exercises are reloaded, as a new name can change their order.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (Form is not { } form) return;

        form.Validate();
        if (!form.IsValid) return;

        if (form.IsCreatingNew)
        {
            await CreateExerciseAsync(form);
            return;
        }

        if (SelectedExercise is not { } exercise) return;

        try
        {
            await _wellnessExerciseService.UpdateExerciseAsync(exercise.Id, form.ToRequest(), _closeTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save the wellness exercise with ID {Id}", exercise.Id);
            await ShowErrorAsync("Wellness.Error.ExerciseSaveFailed");
            return;
        }

        try
        {
            // selecting the saved exercise again gives a clean form
            await LoadExercisesAsync(exercise.Id);
            IsEditing = false;
        }
        catch (OperationCanceledException)
        {
            // the editor was closed while loading
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload the wellness exercises after saving");
            await ShowErrorAsync("Wellness.Error.LoadFailed");
        }
    }

    /// <summary>
    /// Stops editing. Unsaved changes are dropped once the user confirms, by loading the stored exercise into a new
    /// form. A new exercise is dropped entirely, and the exercise selected before it is shown again.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsEditing))]
    private async Task Discard()
    {
        if (Form is { IsDirty: true } && !await ConfirmDiscardAsync()) return;

        if (Form is { IsCreatingNew: true })
        {
            SelectedExercise = _exerciseBeforeDraft;
            _exerciseBeforeDraft = null;

            // without a previous exercise, the selection doesn't change and the form has to be cleared here
            if (SelectedExercise is null) Form = null;
        }
        else if (Form is { IsDirty: true } && SelectedExercise is { } exercise)
        {
            Form = new ExerciseFormViewModel(exercise.Exercise);
        }

        IsEditing = false;
    }

    /// <summary>
    /// Deletes the selected exercise once the user confirms, and selects the one that takes its place in the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeleteExercise))]
    private async Task DeleteExercise()
    {
        if (SelectedExercise is not { } exercise || !await ConfirmDeleteAsync(exercise)) return;

        try
        {
            await _wellnessExerciseService.DeleteExerciseAsync(exercise.Id, _closeTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete the wellness exercise with ID {Id}", exercise.Id);
            await ShowErrorAsync("Wellness.Error.ExerciseDeleteFailed");
            return;
        }

        var index = Exercises.IndexOf(exercise);
        Exercises.Remove(exercise);

        // the next exercise moves up into the place of the deleted one; after the last one, the one before it is taken
        SelectedExercise = Exercises.Count == 0 ? null : Exercises[Math.Clamp(index, 0, Exercises.Count - 1)];
    }

    /// <summary>
    /// Adds the missing built-in exercises and selects the first of them. The user is told how many were restored.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRestoreDefaults))]
    private async Task RestoreDefaults()
    {
        int restoredCount;

        try
        {
            restoredCount = await _wellnessExerciseService.RestoreDefaultExercisesAsync(_closeTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore the default wellness exercises");
            await ShowErrorAsync("Wellness.Error.DefaultsRestoreFailed");
            return;
        }

        if (restoredCount == 0)
        {
            _toastMessageService.ShowToastMessage(
                LocalizationService.GetString("Wellness.ToastMessage.NothingToRestore"), ToastMessageType.Info);
            return;
        }

        _toastMessageService.ShowToastMessage(
            string.Format(LocalizationService.GetString("Wellness.ToastMessage.DefaultsRestored"), restoredCount),
            ToastMessageType.Success);

        var knownIds = Exercises.Select(e => e.Id).ToHashSet();

        try
        {
            await LoadExercisesAsync(SelectedExercise?.Id);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload the wellness exercises after restoring the defaults");
            await ShowErrorAsync("Wellness.Error.LoadFailed");
            return;
        }

        SelectedExercise = Exercises.FirstOrDefault(e => !knownIds.Contains(e.Id)) ?? SelectedExercise;
    }

    private bool CanEditExercise() => SelectedExercise is not null && !IsEditing;

    private bool CanSave() => IsEditing && Form is { IsDirty: true, IsValid: true };

    private bool CanCreateNewExercise() => IsInitialized && !IsEditing;

    private bool CanDeleteExercise() => SelectedExercise is not null && !IsEditing;

    private bool CanRestoreDefaults() => IsInitialized && !IsEditing;

    /// <summary>
    /// Stops the work that is still running, including the preview. Called once the editor is no longer shown.
    /// </summary>
    public void Close()
    {
        _closeTokenSource.Cancel();
        Preview.Dispose();
    }

    partial void OnSelectedExerciseChanged(WellnessExerciseViewModel? value)
    {
        Form = value is null ? null : new ExerciseFormViewModel(value.Exercise);
    }

    partial void OnFormChanged(ExerciseFormViewModel? oldValue, ExerciseFormViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnFormPropertyChanged;
            oldValue.DefinitionChanged -= OnFormDefinitionChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnFormPropertyChanged;
            newValue.DefinitionChanged += OnFormDefinitionChanged;
        }

        // a reloaded form of the same exercise, e.g. after saving, keeps the position of the preview; another exercise
        // is shown from its start, paused
        var isOtherExercise = newValue is null || newValue.IsCreatingNew || newValue.Id != oldValue?.Id;
        LoadPreview(reset: isOtherExercise);
    }

    private void OnFormDefinitionChanged(object? sender, EventArgs e) => LoadPreview(reset: false);

    private void OnPreviewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExercisePreviewViewModel.Progress))
        {
            Form?.HighlightStep(Preview.Progress?.StepIndex);
        }
    }

    private void LoadPreview(bool reset)
    {
        if (Form is not { } form)
        {
            Preview.Clear();
            return;
        }

        if (reset) Preview.Reset(form.ToSteps(), form.Type, form.IsRepeating);
        else Preview.Load(form.ToSteps(), form.Type, form.IsRepeating);

        // the progress may not change, but which instruction a step comes from can
        form.HighlightStep(Preview.Progress?.StepIndex);
    }

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ExerciseFormViewModel.IsDirty) or nameof(ExerciseFormViewModel.IsValid))
        {
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task CreateExerciseAsync(ExerciseFormViewModel form)
    {
        WellnessExercise created;

        try
        {
            created = await _wellnessExerciseService.CreateExerciseAsync(form.ToRequest(), _closeTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create a new {Type} wellness exercise", form.Type);
            await ShowErrorAsync("Wellness.Error.ExerciseSaveFailed");
            return;
        }

        var exercise = new WellnessExerciseViewModel(created);

        // keeps the order of the list: by type, then by name like the service sorts them
        var index = Exercises.TakeWhile(e => e.SessionType < exercise.SessionType
            || (e.SessionType == exercise.SessionType
                && string.Compare(e.Name, exercise.Name, StringComparison.CurrentCultureIgnoreCase) <= 0)).Count();

        Exercises.Insert(index, exercise);
        _exerciseBeforeDraft = null;

        // selecting the new exercise replaces the draft with a clean form of the stored exercise
        SelectedExercise = exercise;
        IsEditing = false;
    }

    private async Task LoadExercisesAsync(Guid? selectedExerciseId)
    {
        var exercises = await _wellnessExerciseService.GetExercisesAsync(cancellationToken: _closeTokenSource.Token);

        // the order is stable, so each type keeps the name order of the service
        Exercises.Clear();
        Exercises.AddRange(exercises.OrderBy(e => e.SessionType).Select(e => new WellnessExerciseViewModel(e)));
        SelectedExercise = Exercises.FirstOrDefault(e => e.Id == selectedExerciseId) ?? Exercises.FirstOrDefault();
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        var confirmation = new ConfirmDialogViewModel
        {
            Title = LocalizationService.GetString("Wellness.DiscardChangesDialog.Title"),
            Message = LocalizationService.GetString("Wellness.DiscardChangesDialog.Message"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Wellness.Editor.DiscardButton"),
            IsDestructive = true
        };

        await _dialogService.ShowDialogAsync(confirmation);
        return confirmation.Confirmed;
    }

    private async Task<bool> ConfirmDeleteAsync(WellnessExerciseViewModel exercise)
    {
        var confirmation = new ConfirmDialogViewModel
        {
            Title = LocalizationService.GetString("Wellness.DeleteExerciseDialog.Title"),
            Message = string.Format(LocalizationService.GetString("Wellness.DeleteExerciseDialog.Message"), exercise.Name),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
            IsDestructive = true
        };

        await _dialogService.ShowDialogAsync(confirmation);
        return confirmation.Confirmed;
    }

    private async Task ShowErrorAsync(string messageKey)
    {
        var errorDialog = new ErrorDialogViewModel
        {
            Title = LocalizationService.GetString("Common.Error.Title"),
            Message = LocalizationService.GetString(messageKey)
        };

        await _dialogService.ShowDialogAsync(errorDialog);
    }
}
