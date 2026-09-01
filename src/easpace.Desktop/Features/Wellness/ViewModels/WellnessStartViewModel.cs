// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly IBreathingTechniqueService _breathingTechniqueService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessStartViewModel> _logger;

    [NotifyPropertyChangedFor(nameof(DurationText))] [ObservableProperty]
    private double _selectedSeconds = 300;

    [ObservableProperty] private double _stepSeconds = 60;
    [ObservableProperty] private double _maximumSeconds = 30 * 60;
    [ObservableProperty] private double _minimumSeconds = 60;
    [ObservableProperty] private bool _isTimerChecked = true;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    private bool _isBreathingChecked = true;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    private bool _isMeditationChecked;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    private BreathingTechniqueViewModel? _selectedBreathingTechniqueViewModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowNoBreathingTechniques))]
    [NotifyPropertyChangedFor(nameof(ShowNoSessionEntries))]
    private bool _isInitialized;

    [ObservableProperty] private bool _isLoading = true;
    
    public AvaloniaList<WellnessSessionEntryViewModel> WellnessSessionEntries { get; } = [];

    private bool _isInitializationRunning;
    
    public bool HasSessionEntries => WellnessSessionEntries.Count > 0;
    
    public bool HasBreathingTechniques => BreathingTechniques.Count > 0;
    
    public bool ShowNoBreathingTechniques => IsInitialized && !HasBreathingTechniques;

    public bool ShowNoSessionEntries => IsInitialized && !HasSessionEntries;

    private bool CanStartSession()
    {
        if (IsMeditationChecked) return true;

        return IsBreathingChecked && SelectedBreathingTechniqueViewModel != null;
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
            var timeSpan = TimeSpan.FromSeconds(SelectedSeconds);

            if (IsBreathingChecked && SelectedBreathingTechniqueViewModel != null)
            {
                // format string as hh:mm:ss if an hour or more, otherwise mm:ss
                var timeString = timeSpan.TotalHours >= 1
                    ? timeSpan.ToString(@"hh\:mm\:ss")
                    : timeSpan.ToString(@"mm\:ss");

                var cycles = (int)(SelectedSeconds / StepSeconds);

                var cyclesText = string.Empty;

                // get localized cycle text based on cycle count
                if (cycles == 1)
                {
                    cyclesText = LocalizationService.GetString("Wellness.Session.OneCycle");
                }
                else if (cycles > 1)
                {
                    cyclesText = string.Format(LocalizationService.GetString("Wellness.Session.Cycles"), cycles);
                }

                return $"{timeString} ({cyclesText})";
            }

            // fallback to standard minutes formatting for meditation
            var minutes = (int)(SelectedSeconds / 60);
            var localizationKey = minutes == 1 ? "Common.Time.OneMinute" : "Common.Time.Minutes";
            return string.Format(LocalizationService.GetString(localizationKey), minutes);
        }
    }

    /// <summary>
    /// Gets the collection of available breathing techniques.
    /// </summary>
    public AvaloniaList<BreathingTechniqueViewModel> BreathingTechniques { get; } = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessStartViewModel"/> class.
    /// </summary>
    public WellnessStartViewModel(
        IWellnessSessionEntryService wellnessSessionEntryService,
        IBreathingTechniqueService breathingTechniqueService,
        IDialogService dialogService,
        ILogger<WellnessStartViewModel> logger)
    {
        _wellnessSessionEntryService = wellnessSessionEntryService;
        _breathingTechniqueService = breathingTechniqueService;
        _dialogService = dialogService;
        _logger = logger;

        WellnessSessionEntries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasSessionEntries));
            OnPropertyChanged(nameof(ShowNoSessionEntries));
        };
        
        BreathingTechniques.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasBreathingTechniques));
            OnPropertyChanged(nameof(ShowNoBreathingTechniques));
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
            await LoadBreathingTechniques();

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
            _logger.LogError(ex, "An error occurred while attempting to delete wellness session entry {EntryId}", entry.Id);
            
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
        TimeSpan targetDuration;
        BreathingTechniqueConfiguration? breathingTechniqueConfiguration = null;

        // configure the breathing technique parameters if applicable
        if (IsBreathingChecked && SelectedBreathingTechniqueViewModel != null)
        {
            // calculate the total duration of a single breathing cycle in seconds
            var cycleDurationSeconds = SelectedBreathingTechniqueViewModel.Phases.Sum(p => p.DurationSeconds);

            int cycles;

            if (IsTimerChecked)
            {
                cycles = (int)Math.Round(SelectedSeconds / cycleDurationSeconds);
                targetDuration = TimeSpan.FromSeconds(cycles * cycleDurationSeconds);
            }
            else
            {
                cycles = SelectedBreathingTechniqueViewModel.Cycles;
                targetDuration = TimeSpan.Zero;
            }

            breathingTechniqueConfiguration = new BreathingTechniqueConfiguration(
                BreathingTechnique: SelectedBreathingTechniqueViewModel.BreathingTechnique,
                Cycles: cycles
            );
        }
        else
        {
            targetDuration = IsTimerChecked ? TimeSpan.FromSeconds(SelectedSeconds) : TimeSpan.Zero;
        }

        // determine session type
        var sessionType = WellnessSessionType.Breathing;

        if (IsMeditationChecked)
        {
            sessionType = WellnessSessionType.Meditation;
        }

        // assemble the final configuration payload
        var sessionConfiguration = new WellnessSessionConfiguration(
            SessionType: sessionType,
            TargetDuration: targetDuration,
            IsTimerSet: IsTimerChecked,
            BreathingTechniqueConfiguration: breathingTechniqueConfiguration
        );

        SessionStarted?.Invoke(this, sessionConfiguration);
    }

    #endregion

    #region Private Helper Methods

    private async Task LoadWellnessSessionEntries()
    {
        var sessionEntries = await _wellnessSessionEntryService.GetWellnessSessionEntriesAsync();
        
        var sessionEntryViewModels = sessionEntries.Select(sessionEntry => new WellnessSessionEntryViewModel(sessionEntry));
        
        WellnessSessionEntries.AddRange(sessionEntryViewModels);
    }

    private async Task LoadBreathingTechniques()
    {
        var techniques = await _breathingTechniqueService.GetBreathingTechniquesAsync();
        
        var techniqueViewModels = techniques.Select(t => new BreathingTechniqueViewModel(t));
        
        BreathingTechniques.AddRange(techniqueViewModels);
        
        SelectedBreathingTechniqueViewModel = BreathingTechniques.FirstOrDefault();
    }

    /// <summary>
    /// Triggered automatically when the breathing radio button state changes.
    /// </summary>
    partial void OnIsBreathingCheckedChanged(bool value) => UpdateSlider();

    /// <summary>
    /// Triggered automatically when the meditation radio button state changes.
    /// </summary>
    partial void OnIsMeditationCheckedChanged(bool value) => UpdateSlider();

    /// <summary>
    /// Triggered automatically when the selected breathing technique changes.
    /// </summary>
    partial void OnSelectedBreathingTechniqueViewModelChanged(BreathingTechniqueViewModel? value) => UpdateSlider();

    /// <summary>
    /// Recalculates slider limits, steps, and selected value to align with the current session mode.
    /// </summary>
    private void UpdateSlider()
    {
        if (IsBreathingChecked && SelectedBreathingTechniqueViewModel != null)
        {
            StepSeconds = SelectedBreathingTechniqueViewModel.Phases.Sum(p => p.DurationSeconds);

            // calculate minimum cycles required to hit at least one minute
            var minCycles = Math.Ceiling(60.0 / StepSeconds);
            MinimumSeconds = minCycles * StepSeconds;
            
            // calculate maximum cycles with a 10 minute limit
            var maxCycles = Math.Floor(10 * 60.0 / StepSeconds);
            MaximumSeconds = maxCycles * StepSeconds;
        }
        else
        {
            MaximumSeconds = 30 * 60;
            StepSeconds = 60;
            MinimumSeconds = 60;
        }

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