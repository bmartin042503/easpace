// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;
using easpace.Desktop.Services;

namespace easpace.Desktop.ViewModels;

public partial class WellnessConfigurationViewModel : ViewModelBase
{
    #region Fields

    [NotifyPropertyChangedFor(nameof(DurationText))] [ObservableProperty]
    private double _selectedSeconds = 300;

    [ObservableProperty] private double _stepSeconds = 60;
    [ObservableProperty] private double _maximumSeconds = 30 * 60;
    [ObservableProperty] private double _minimumSeconds = 60;
    [ObservableProperty] private bool _isTimerChecked = true;
    [ObservableProperty] private bool _isBreathingChecked = true;
    [ObservableProperty] private bool _isMeditationChecked;
    [ObservableProperty] private BreathingTechnique? _selectedBreathingTechnique;

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

            if (IsBreathingChecked && SelectedBreathingTechnique != null)
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
                    cyclesText = LocalizationService.GetString("Wellness.Slider.OneCycle");
                }
                else if (cycles > 1)
                {
                    cyclesText = string.Format(LocalizationService.GetString("Wellness.Slider.Cycles"), cycles);
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
    public ObservableCollection<BreathingTechnique> BreathingTechniques { get; } = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessConfigurationViewModel"/> class.
    /// </summary>
    public WellnessConfigurationViewModel()
    {
        InitializeStockBreathingTechniques();
        SelectedBreathingTechnique = BreathingTechniques.FirstOrDefault();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Constructs the session configuration and triggers the session start event.
    /// </summary>
    [RelayCommand]
    private void StartSession()
    {
        TimeSpan targetDuration;
        BreathingTechniqueConfiguration? breathingTechniqueConfiguration = null;

        // configure the breathing technique parameters if applicable
        if (IsBreathingChecked && SelectedBreathingTechnique != null)
        {
            // calculate the total duration of a single breathing cycle in seconds
            var cycleDurationSeconds = SelectedBreathingTechnique.Phases.Sum(p => p.DurationSeconds);

            int cycles;

            if (IsTimerChecked)
            {
                cycles = (int)(SelectedSeconds / StepSeconds);
                targetDuration = TimeSpan.FromSeconds(cycles * cycleDurationSeconds);
            }
            else
            {
                cycles = SelectedBreathingTechnique.Cycles;
                targetDuration = TimeSpan.Zero;
            }

            breathingTechniqueConfiguration = new BreathingTechniqueConfiguration(
                BreathingTechnique: SelectedBreathingTechnique,
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
    partial void OnSelectedBreathingTechniqueChanged(BreathingTechnique? value) => UpdateSlider();

    /// <summary>
    /// Recalculates slider limits, steps, and selected value to align with the current session mode.
    /// </summary>
    private void UpdateSlider()
    {
        if (IsBreathingChecked && SelectedBreathingTechnique != null)
        {
            MaximumSeconds = 20 * 60;
            StepSeconds = SelectedBreathingTechnique.Phases.Sum(p => p.DurationSeconds);

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
    }

    private void InitializeStockBreathingTechniques()
    {
        // TODO: retrieve these from the database instead of hardcoding
        BreathingTechniques.Add(
            new BreathingTechnique
            {
                Name = "Box Breathing",
                Description = "Box Breathing Description",
                Phases =
                [
                    new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 4 },
                    new BreathingPhase { Type = BreathingPhaseType.HoldIn, DurationSeconds = 4 },
                    new BreathingPhase { Type = BreathingPhaseType.Exhale, DurationSeconds = 4 },
                    new BreathingPhase { Type = BreathingPhaseType.HoldOut, DurationSeconds = 4 }
                ],
                Cycles = 4
            }
        );
    }

    #endregion
}