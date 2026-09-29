// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
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
/// Lets the user browse and manage the wellness exercises in place of the start view.
/// </summary>
internal partial class WellnessExerciseEditorViewModel : ViewModelBase
{
    private readonly IWellnessExerciseService _wellnessExerciseService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessExerciseEditorViewModel> _logger;
    private readonly Guid? _initialExerciseId;

    // cancels loading that is still running when the editor gets closed
    private readonly CancellationTokenSource _closeTokenSource = new();

    private bool _isInitializationRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    private bool _isInitialized;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CycleDurationText))]
    [NotifyPropertyChangedFor(nameof(SelectedSteps))]
    private WellnessExerciseViewModel? _selectedExercise;

    /// <summary>
    /// Gets the exercises of every session type: breathing first, then meditation, each sorted by name.
    /// </summary>
    public AvaloniaList<WellnessExerciseViewModel> Exercises { get; } = [];

    public bool HasExercises => Exercises.Count > 0;
    public bool ShowNoExercises => IsInitialized && !HasExercises;

    public string CycleDurationText => SelectedExercise is { } exercise
        ? string.Format(LocalizationService.GetString("Wellness.Editor.Label.OneCycle"), FormatDuration(exercise.CycleSeconds))
        : string.Empty;

    /// <summary>
    /// Gets the numbered steps of the selected exercise for the read-only summary.
    /// </summary>
    public IReadOnlyList<ExerciseStepSummary> SelectedSteps =>
        SelectedExercise?.Steps
            .Select((step, index) => new ExerciseStepSummary(index + 1, step.Text, FormatDuration(step.DurationSeconds)))
            .ToList() ?? [];

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
            var exercises = await _wellnessExerciseService.GetExercisesAsync(cancellationToken: _closeTokenSource.Token);

            // the order is stable, so each type keeps the name order of the service
            Exercises.AddRange(exercises.OrderBy(e => e.SessionType).Select(e => new WellnessExerciseViewModel(e)));
            SelectedExercise = Exercises.FirstOrDefault(e => e.Id == _initialExerciseId) ?? Exercises.FirstOrDefault();

            IsInitialized = true;
        }
        catch (OperationCanceledException)
        {
            // the editor was closed while loading
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the wellness exercises for the editor");

            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Wellness.Error.LoadFailed")
            };

            await _dialogService.ShowDialogAsync(errorDialog);
        }
        finally
        {
            _isInitializationRunning = false;
        }
    }

    [RelayCommand]
    private void NavigateBack() => Closed?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Stops the work that is still running. Called once the editor is no longer shown.
    /// </summary>
    public void Close() => _closeTokenSource.Cancel();

    private static string FormatDuration(int seconds)
    {
        var duration = TimeSpan.FromSeconds(seconds);
        return duration.ToString(duration.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
    }
}

/// <summary>
/// Represents a step of the selected exercise as shown in the editor's read-only summary.
/// </summary>
/// <param name="Number">The 1-based position of the step within its cycle.</param>
/// <param name="Text">The instruction text of the step.</param>
/// <param name="DurationText">The formatted duration of the step.</param>
internal sealed record ExerciseStepSummary(int Number, string Text, string DurationText);
