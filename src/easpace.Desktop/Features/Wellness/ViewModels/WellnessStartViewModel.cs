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

    // sessions default to about five minutes and never run longer than an hour
    private const int DefaultSessionSeconds = 5 * 60;
    private const int MaximumSessionSeconds = 60 * 60;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    private int _selectedCycles = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText))]
    private int _maximumCycles = 1;

    public IEnumerable<WellnessSessionType> SessionTypes { get; } = Enum.GetValues<WellnessSessionType>();
    
    [ObservableProperty]
    private WellnessSessionType _selectedSessionType;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    [NotifyPropertyChangedFor(nameof(ShowCycleSelector))]
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
    /// Gets whether the number of cycles can be chosen, which only repeating exercises allow.
    /// </summary>
    public bool ShowCycleSelector => SelectedExercise is { IsRepeating: true };

    // a session needs at least one step, and a repeating exercise at least one cycle
    private bool CanStartSession() =>
        SelectedExercise is { Steps.Count: > 0 } exercise && (!exercise.IsRepeating || SelectedCycles >= 1);

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the user initiates a new wellness session.
    /// </summary>
    public event EventHandler<WellnessSessionConfiguration>? SessionStarted;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the session length: approximate for the chosen cycles of a repeating exercise, fixed for one that runs once.
    /// </summary>
    public string DurationText
    {
        get
        {
            if (SelectedExercise is not { CycleSeconds: > 0 } exercise) return string.Empty;

            var duration = FormatDuration(TimeSpan.FromSeconds(GetCycles(exercise) * exercise.CycleSeconds));

            return exercise.IsRepeating
                ? string.Format(LocalizationService.GetString("Wellness.Label.ApproxDuration"), duration)
                : $"{LocalizationService.GetString("Wellness.Label.Duration")}: {duration}";
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

            UpdateCycleSelector();

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
        // the command can also be executed directly, so the guard applies here as well
        if (!CanStartSession() || SelectedExercise is not { } exercise) return;

        // assemble the final configuration payload
        var sessionConfiguration = new WellnessSessionConfiguration(
            SessionType: exercise.SessionType,
            ExerciseId: exercise.Id,
            ExerciseName: exercise.Name,
            Steps: exercise.Steps,
            Cycles: GetCycles(exercise),
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
    /// Gets the number of cycles to run: the selection within its bounds for a repeating exercise, otherwise one.
    /// </summary>
    private int GetCycles(WellnessExerciseViewModel exercise) =>
        exercise.IsRepeating ? Math.Clamp(SelectedCycles, 1, MaximumCycles) : 1;

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
    partial void OnSelectedExerciseChanged(WellnessExerciseViewModel? value) => UpdateCycleSelector();

    /// <summary>
    /// Applies the cycle bounds of the selected exercise and selects about five minutes of cycles.
    /// </summary>
    private void UpdateCycleSelector()
    {
        if (SelectedExercise is { IsRepeating: true, CycleSeconds: > 0 } exercise)
        {
            // the maximum goes first, otherwise the stepper would clip the new selection to the previous maximum;
            // a single cycle is always allowed, even if it's longer than the cap
            MaximumCycles = Math.Max(1, MaximumSessionSeconds / exercise.CycleSeconds);
            SelectedCycles = Math.Clamp((int)Math.Round((double)DefaultSessionSeconds / exercise.CycleSeconds), 1, MaximumCycles);
        }

        OnPropertyChanged(nameof(DurationText));
    }

    #endregion
}