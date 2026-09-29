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
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

/// <summary>
/// Lets the user browse and edit the wellness exercises in place of the start view.
/// </summary>
internal partial class WellnessExerciseEditorViewModel : ViewModelBase
{
    private readonly IWellnessExerciseService _wellnessExerciseService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessExerciseEditorViewModel> _logger;
    private readonly Guid? _initialExerciseId;

    // cancels work that is still running when the editor gets closed
    private readonly CancellationTokenSource _closeTokenSource = new();

    private bool _isInitializationRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    private bool _isInitialized;

    [ObservableProperty] private WellnessExerciseViewModel? _selectedExercise;

    /// <summary>
    /// Gets the form of the selected exercise, or <c>null</c> when nothing is selected.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChangeSelection))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DiscardCommand))]
    private ExerciseFormViewModel? _form;

    /// <summary>
    /// Gets the exercises of every session type: breathing first, then meditation, each sorted by name.
    /// </summary>
    public AvaloniaList<WellnessExerciseViewModel> Exercises { get; } = [];

    public bool HasExercises => Exercises.Count > 0;
    public bool ShowNoExercises => IsInitialized && !HasExercises;

    /// <summary>
    /// Gets whether another exercise can be selected, which unsaved changes prevent.
    /// </summary>
    public bool CanChangeSelection => HasExercises && Form is not { IsDirty: true };

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
        ILogger<WellnessExerciseEditorViewModel> logger,
        Guid? selectedExerciseId)
    {
        _wellnessExerciseService = wellnessExerciseService;
        _dialogService = dialogService;
        _logger = logger;
        _initialExerciseId = selectedExerciseId;

        Exercises.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasExercises));
            OnPropertyChanged(nameof(ShowNoExercises));
            OnPropertyChanged(nameof(CanChangeSelection));
        };
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
    /// Saves the form and reloads the exercises, as a new name can change their order.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (Form is not { } form || SelectedExercise is not { } exercise) return;

        form.Validate();
        if (!form.IsValid) return;

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
            await ShowErrorAsync("Wellness.Editor.Error.SaveFailed");
            return;
        }

        try
        {
            // selecting the saved exercise again gives a clean form
            await LoadExercisesAsync(exercise.Id);
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
    /// Drops the unsaved changes by loading the stored exercise into a new form.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDiscard))]
    private void Discard()
    {
        Form = SelectedExercise is { } exercise ? new ExerciseFormViewModel(exercise.Exercise) : null;
    }

    private bool CanSave() => Form is { IsDirty: true, IsValid: true };

    private bool CanDiscard() => Form is { IsDirty: true };

    /// <summary>
    /// Stops the work that is still running. Called once the editor is no longer shown.
    /// </summary>
    public void Close() => _closeTokenSource.Cancel();

    partial void OnSelectedExerciseChanged(WellnessExerciseViewModel? value)
    {
        Form = value is null ? null : new ExerciseFormViewModel(value.Exercise);
    }

    partial void OnFormChanged(ExerciseFormViewModel? oldValue, ExerciseFormViewModel? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= OnFormPropertyChanged;
        if (newValue is not null) newValue.PropertyChanged += OnFormPropertyChanged;
    }

    private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ExerciseFormViewModel.IsDirty) or nameof(ExerciseFormViewModel.IsValid))) return;

        OnPropertyChanged(nameof(CanChangeSelection));
        SaveCommand.NotifyCanExecuteChanged();
        DiscardCommand.NotifyCanExecuteChanged();
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
            Title = LocalizationService.GetString("Wellness.Editor.DiscardDialog.Title"),
            Message = LocalizationService.GetString("Wellness.Editor.DiscardDialog.Message"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Wellness.Editor.Button.Discard"),
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
