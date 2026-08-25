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
using easpace.Desktop.Services;
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

    private bool _isInitialized;
    
    public AvaloniaList<WellnessSessionEntryViewModel> WellnessSessionEntries { get; } = [];
    
    public bool HasSessionEntries => WellnessSessionEntries.Count > 0;
    
    public bool HasBreathingTechniques => BreathingTechniques.Count > 0;

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

        WellnessSessionEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSessionEntries));
        BreathingTechniques.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBreathingTechniques));
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            await LoadWellnessSessionEntries();
            await LoadBreathingTechniques();
            _isInitialized = true;
            
            UpdateSlider();
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
                cycles = (int)(SelectedSeconds / StepSeconds);
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
            MaximumSeconds = 20 * 60;
            StepSeconds = SelectedBreathingTechniqueViewModel.Phases.Sum(p => p.DurationSeconds);

            // calculate minimum cycles required to hit at least one minute
            var minCycles = Math.Ceiling(60.0 / StepSeconds);
            MinimumSeconds = minCycles * StepSeconds;
        }
        else
        {
            MaximumSeconds = 60 * 60;
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
            var maxCycles = Math.Floor(MaximumSeconds / StepSeconds);
            newSelectedSeconds = maxCycles * StepSeconds;
        }

        SelectedSeconds = newSelectedSeconds;
        OnPropertyChanged(nameof(DurationText));
    }

    #endregion
}