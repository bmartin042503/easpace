// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Repositories;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal partial class WellnessStartViewModel : ViewModelBase
{
    #region Fields

    private readonly IWellnessSessionEntryRepository _wellnessSessionEntryRepository;
    private readonly IWellnessExerciseRepository _wellnessExerciseRepository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessStartViewModel> _logger;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DurationText))]
    private int _selectedCycles = 1;

    [ObservableProperty] private double _stepSeconds = 60;
    [ObservableProperty] private double _maximumSeconds = 30 * 60;
    [ObservableProperty] private double _minimumSeconds = 60;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    private WellnessExerciseViewModel? _selectedExerciseViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    [NotifyPropertyChangedFor(nameof(ShowNoSessionEntries))]
    private bool _isInitialized;

    [ObservableProperty] private bool _isLoading = true;

    public AvaloniaList<WellnessSessionEntryViewModel> WellnessSessionEntries { get; } = [];

    private bool _isInitializationRunning;

    public bool HasSessionEntries => WellnessSessionEntries.Count > 0;
    public bool HasExercises => WellnessExercises.Count > 0;
    public bool ShowNoExercises => IsInitialized && !HasExercises;
    public bool ShowNoSessionEntries => IsInitialized && !HasSessionEntries;

    private bool CanStartSession()
    {
        return SelectedExerciseViewModel != null;
    }

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
            if (SelectedExerciseViewModel == null) return string.Empty;
            var oneCycleSeconds = SelectedExerciseViewModel.Instructions.Sum(i => i.DurationSeconds);
            var exerciseDuration = TimeSpan.FromSeconds(oneCycleSeconds * SelectedCycles);

            // format string as hh:mm:ss if an hour or more, otherwise mm:ss
            var timeString = exerciseDuration.TotalHours >= 1
                ? exerciseDuration.ToString(@"hh\:mm\:ss")
                : exerciseDuration.ToString(@"mm\:ss");

            var cyclesText = string.Empty;

            // get localized cycle text based on cycle count
            if (SelectedCycles == 1)
            {
                cyclesText = LocalizationService.GetString("Wellness.Session.OneCycle");
            }
            else if (SelectedCycles > 1)
            {
                cyclesText = string.Format(LocalizationService.GetString("Wellness.Session.Cycles"), SelectedCycles);
            }

            return $"{timeString} ({cyclesText})";
        }
    }

    /// <summary>
    /// Gets the collection of available exercises.
    /// </summary>
    public AvaloniaList<WellnessExerciseViewModel> WellnessExercises { get; } = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessStartViewModel"/> class.
    /// </summary>
    public WellnessStartViewModel(
        IWellnessSessionEntryRepository wellnessSessionEntryRepository,
        IWellnessExerciseRepository wellnessExerciseRepository,
        IDialogService dialogService,
        ILogger<WellnessStartViewModel> logger)
    {
        _wellnessSessionEntryRepository = wellnessSessionEntryRepository;
        _wellnessExerciseRepository = wellnessExerciseRepository;
        _dialogService = dialogService;
        _logger = logger;

        WellnessSessionEntries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSessionEntries));
            OnPropertyChanged(nameof(ShowNoSessionEntries));
        };

        WellnessExercises.CollectionChanged += (_, _) =>
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
            await LoadWellnessExercises();

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration data and initialize wellness start view");

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
                var isDeleted = await _wellnessSessionEntryRepository.DeleteWellnessSessionEntryAsync(entry.Id);

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
    /// Constructs the session configuration and triggers the session start event.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartSession))]
    private void StartSession()
    {
        if (SelectedExerciseViewModel == null) return;

        var sessionConfiguration = new WellnessSessionConfiguration(
            SelectedExerciseViewModel.Exercise,
            SelectedCycles
        );

        SessionStarted?.Invoke(this, sessionConfiguration);
    }

    #endregion

    #region Private Helper Methods

    private async Task LoadWellnessSessionEntries()
    {
        var sessionEntries = await _wellnessSessionEntryRepository.GetWellnessSessionEntriesAsync();

        var sessionEntryViewModels =
            sessionEntries.Select(sessionEntry => new WellnessSessionEntryViewModel(sessionEntry));

        WellnessSessionEntries.AddRange(sessionEntryViewModels);
    }

    private async Task LoadWellnessExercises()
    {
        var exercises = await _wellnessExerciseRepository.GetWellnessExercisesAsync();
        var exerciseViewModels = exercises.Select(e => new WellnessExerciseViewModel(e));
        WellnessExercises.AddRange(exerciseViewModels);
        SelectedExerciseViewModel = WellnessExercises.FirstOrDefault();
    }

    #endregion
}