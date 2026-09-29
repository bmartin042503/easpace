// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal partial class WellnessStartViewModel : ViewModelBase
{
    #region Fields

    private readonly IWellnessSessionEntryService _wellnessSessionEntryService;
    private readonly IWellnessExerciseService _wellnessExerciseService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessStartViewModel> _logger;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(DurationText))]
    private double _selectedSeconds = 300;

    [ObservableProperty] private double _stepSeconds = 60;
    [ObservableProperty] private double _maximumSeconds = 30 * 60;
    [ObservableProperty] private double _minimumSeconds = 60;
    
    public IEnumerable<WellnessSessionType> SessionTypes { get; } = Enum.GetValues<WellnessSessionType>();
    
    [ObservableProperty]
    private WellnessSessionType _selectedSessionType;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    [NotifyPropertyChangedFor(nameof(ShowDurationSlider))]
    private WellnessExerciseViewModel? _selectedExercise;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    [NotifyPropertyChangedFor(nameof(ShowNoSessionEntries))]
    private bool _isInitialized;

    [ObservableProperty] private bool _isLoading = true;

    public AvaloniaList<WellnessSessionEntryViewModel> WellnessSessionEntries { get; } = [];

    // exercises of every session type; the list only shows the selected type
    private IReadOnlyList<WellnessExerciseViewModel> _allExercises = [];

    private bool _isInitializationRunning;

    public bool HasSessionEntries => WellnessSessionEntries.Count > 0;
    public bool HasExercises => Exercises.Count > 0;
    public bool ShowNoExercises => IsInitialized && !HasExercises;
    public bool ShowNoSessionEntries => IsInitialized && !HasSessionEntries;

    /// <summary>
    /// Gets whether the length of the session can be chosen, which only repeating exercises allow.
    /// </summary>
    public bool ShowDurationSlider => SelectedExercise is { IsRepeating: true };

    // a session can't run without at least one step
    private bool CanStartSession() => SelectedExercise is { Steps.Count: > 0 };

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the user initiates a new wellness session.
    /// </summary>
    public event EventHandler<WellnessSessionConfiguration>? SessionStarted;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the formatted duration text to be displayed on the UI based on current selections.
    /// </summary>
    public string DurationText
    {
        get
        {
            if (SelectedExercise is not { CycleSeconds: > 0 } exercise) return string.Empty;

            // exercises that don't repeat run once, so their length is fixed
            if (!exercise.IsRepeating)
            {
                return $"{LocalizationService.GetString("Wellness.Label.Duration")}: " +
                       FormatDuration(TimeSpan.FromSeconds(exercise.CycleSeconds));
            }

            var cycles = GetSelectedCycles(exercise);

            // get localized cycle text based on cycle count
            var cyclesText = cycles == 1
                ? LocalizationService.GetString("Wellness.Session.OneCycle")
                : string.Format(LocalizationService.GetString("Wellness.Session.Cycles"), cycles);

            return $"{FormatDuration(TimeSpan.FromSeconds(cycles * exercise.CycleSeconds))} ({cyclesText})";
        }
    }

    /// <summary>
    /// Gets the exercises of the selected session type.
    /// </summary>
    public AvaloniaList<WellnessExerciseViewModel> Exercises { get; } = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessStartViewModel"/> class.
    /// </summary>
    public WellnessStartViewModel(
        IWellnessSessionEntryService wellnessSessionEntryService,
        IWellnessExerciseService wellnessExerciseService,
        IDialogService dialogService,
        ILogger<WellnessStartViewModel> logger)
    {
        _wellnessSessionEntryService = wellnessSessionEntryService;
        _wellnessExerciseService = wellnessExerciseService;
        _dialogService = dialogService;
        _logger = logger;

        WellnessSessionEntries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSessionEntries));
            OnPropertyChanged(nameof(ShowNoSessionEntries));
        };

        Exercises.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasExercises));
            OnPropertyChanged(nameof(ShowNoExercises));
        };
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsInitialized || _isInitializationRunning)
        {
            return;
        }

        _isInitializationRunning = true;
        IsLoading = true;

        try
        {
            await LoadWellnessSessionEntries();
            await LoadExercises();

            UpdateSlider();

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load configuration data and initialize wellness start view");

            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Wellness.Error.LoadFailed")
            };

            await _dialogService.ShowDialogAsync(errorDialog);
        }
        finally
        {
            IsLoading = false;
            _isInitializationRunning = false;
        }
    }

    [RelayCommand]
    private async Task DeleteEntry(object parameter)
    {
        if (parameter is not WellnessSessionEntryViewModel entry) return;

        try
        {
            var confirmation = new ConfirmDialogViewModel
            {
                Title = LocalizationService.GetString("Wellness.DeleteSessionDialog.Title"),
                Message = LocalizationService.GetString("Wellness.DeleteSessionDialog.Message"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
                IsDestructive = true,
            };

            await _dialogService.ShowDialogAsync(confirmation);

            if (confirmation.Confirmed)
            {
                var isDeleted = await _wellnessSessionEntryService.DeleteWellnessSessionEntryAsync(entry.Id);

                if (!isDeleted) return;

                WellnessSessionEntries.Remove(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while attempting to delete wellness session entry {EntryId}",
                entry.Id);

            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Wellness.Error.DeleteFailed")
            };

            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    /// <summary>
    /// Constructs the session configuration from the selected exercise and triggers the session start event.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartSession))]
    private void StartSession()
    {
        if (SelectedExercise is not { Steps.Count: > 0 } exercise) return;

        // exercises that don't repeat run exactly once
        var cycles = exercise.IsRepeating ? GetSelectedCycles(exercise) : 1;

        // assemble the final configuration payload
        var sessionConfiguration = new WellnessSessionConfiguration(
            SessionType: exercise.SessionType,
            ExerciseId: exercise.Id,
            ExerciseName: exercise.Name,
            Steps: exercise.Steps,
            Cycles: cycles,
            IsRepeating: exercise.IsRepeating
        );

        SessionStarted?.Invoke(this, sessionConfiguration);
    }

    #endregion

    #region Private Helper Methods

    private async Task LoadWellnessSessionEntries()
    {
        var sessionEntries = await _wellnessSessionEntryService.GetWellnessSessionEntriesAsync();

        var sessionEntryViewModels =
            sessionEntries.Select(sessionEntry => new WellnessSessionEntryViewModel(sessionEntry));

        WellnessSessionEntries.AddRange(sessionEntryViewModels);
    }

    private async Task LoadExercises()
    {
        var exercises = await _wellnessExerciseService.GetExercisesAsync();
        _allExercises = exercises.Select(e => new WellnessExerciseViewModel(e)).ToList();
        UpdateExercises();
    }

    /// <summary>
    /// Lists the exercises of the selected session type and selects the first one.
    /// </summary>
    private void UpdateExercises()
    {
        Exercises.Clear();
        Exercises.AddRange(_allExercises.Where(e => e.SessionType == SelectedSessionType));
        SelectedExercise = Exercises.FirstOrDefault();
    }

    /// <summary>
    /// Gets the number of cycles that the slider selection amounts to, which is at least one.
    /// </summary>
    private int GetSelectedCycles(WellnessExerciseViewModel exercise) =>
        Math.Max(1, (int)Math.Round(SelectedSeconds / exercise.CycleSeconds));

    // format as hh:mm:ss if an hour or more, otherwise mm:ss
    private static string FormatDuration(TimeSpan duration) =>
        duration.ToString(duration.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");

    /// <summary>
    /// Triggered automatically when the selected session type changes.
    /// </summary>
    partial void OnSelectedSessionTypeChanged(WellnessSessionType value) => UpdateExercises();

    /// <summary>
    /// Triggered automatically when the selected exercise changes.
    /// </summary>
    partial void OnSelectedExerciseChanged(WellnessExerciseViewModel? value) => UpdateSlider();

    /// <summary>
    /// Recalculates slider limits, steps, and selected value to align with the selected exercise.
    /// </summary>
    private void UpdateSlider()
    {
        // only repeating exercises have a slider, and it moves in whole cycles
        if (SelectedExercise is not { IsRepeating: true, CycleSeconds: > 0 } exercise)
        {
            OnPropertyChanged(nameof(DurationText));
            return;
        }

        StepSeconds = exercise.CycleSeconds;

        // calculate maximum cycles with a 10-minute limit for breathing and 30 minutes for meditation,
        // but always allow at least one cycle
        var maximumSessionSeconds = exercise.SessionType == WellnessSessionType.Breathing ? 10 * 60 : 30 * 60;
        var maxCycles = Math.Max(1, Math.Floor(maximumSessionSeconds / StepSeconds));

        // calculate minimum cycles required to hit at least one minute, without exceeding the maximum
        var minCycles = Math.Min(maxCycles, Math.Ceiling(60.0 / StepSeconds));

        MinimumSeconds = minCycles * StepSeconds;
        MaximumSeconds = maxCycles * StepSeconds;

        // round current selection to the nearest valid step interval
        var targetCycles = Math.Round(SelectedSeconds / StepSeconds);
        var newSelectedSeconds = targetCycles * StepSeconds;

        // enforce slider bounds safely
        if (newSelectedSeconds < MinimumSeconds)
        {
            newSelectedSeconds = MinimumSeconds;
        }
        else if (newSelectedSeconds > MaximumSeconds)
        {
            newSelectedSeconds = MaximumSeconds;
        }

        SelectedSeconds = newSelectedSeconds;
        OnPropertyChanged(nameof(DurationText));
    }

    #endregion
}