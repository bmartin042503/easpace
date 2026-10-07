// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal partial class WellnessSessionViewModel : ViewModelBase
{
    #region Fields

    private DateTimeOffset _startDate;

    private readonly WellnessSessionConfiguration _sessionConfiguration;
    private readonly IWellnessExercisePlayer _wellnessExercisePlayer;

    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _instructionText = string.Empty;
    [ObservableProperty] private string _phaseSecondsText = string.Empty;
    [ObservableProperty] private string _timerText = "00:00";
    [ObservableProperty] private double _breathingCircleSize = 96;

    [ObservableProperty]
    private string _timerToggleButtonText = LocalizationService.GetString("Wellness.Button.PauseSession");

    public bool IsBreathing { get; set; }
    public bool ShowWellnessTimer { get; init; }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the wellness session has ended or is manually stopped.
    /// </summary>
    public event EventHandler<CreateWellnessSessionEntryRequest>? SessionEnded;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessSessionViewModel"/> class.
    /// </summary>
    /// <param name="preferencesService">The preferences service to load settings related to the session.</param>
    /// <param name="sessionConfiguration">The configuration parameters for the current session.</param>
    public WellnessSessionViewModel(
        IPreferencesService preferencesService,
        WellnessSessionConfiguration sessionConfiguration)
    {
        _sessionConfiguration = sessionConfiguration;

       // TODO: initialize exercise player
       _wellnessExercisePlayer = new WellnessExercisePlayer();

        ShowWellnessTimer = preferencesService.ReadPreference<bool>(PreferenceKey.WellnessShowTimer);

        // _wellnessExercisePlayer.TimerTick += OnSessionManagerTimerTick;
        // _wellnessExercisePlayer.BreathingCircleAnimationTimerTick += OnBreathingCircleAnimationTimerTick;
        // _wellnessExercisePlayer.TimerFinished += OnSessionManagerTimerFinished;
        
        // TODO: initialize first instruction of the exercise and start exercise player with the configuration
        
        // configure specific session type properties
        // _wellnessExercisePlayer.Start(_sessionConfiguration.TargetCycleCount);
        _startDate = DateTimeOffset.Now;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Pauses or resumes the ongoing wellness session timers and animations.
    /// </summary>
    [RelayCommand]
    private void ToggleSessionTimer()
    {
        if (IsPaused)
        {
            _wellnessExercisePlayer.Resume();

            IsPaused = false;
            TimerToggleButtonText = LocalizationService.GetString("Wellness.Button.PauseSession");
        }
        else
        {
            _wellnessExercisePlayer.Pause();

            IsPaused = true;
            TimerToggleButtonText = LocalizationService.GetString("Wellness.Button.ResumeSession");
        }
    }

    /// <summary>
    /// Manually stops the current session and proceeds to the ending screen.
    /// </summary>
    [RelayCommand]
    private void StopSession()
    {
        _wellnessExercisePlayer.Stop();

        FinishSession();
    }

    #endregion

    #region Private Helper Methods

    private void OnSessionManagerTimerTick(object? sender, SessionTexts sessionTexts)
    {
        InstructionText = sessionTexts.InstructionText;
        TimerText = sessionTexts.TimerText;
        PhaseSecondsText = sessionTexts.PhaseSecondsText ?? string.Empty;
    }

    private void OnBreathingCircleAnimationTimerTick(object? sender, double breathingCircleSize)
    {
        BreathingCircleSize = breathingCircleSize;
    }

    /// <summary>
    /// Concludes the session, stops all timers, aggregates session data, and triggers the end event.
    /// </summary>
    private void OnSessionManagerTimerFinished(object? sender, EventArgs e)
    {
        /*
        _wellnessExercisePlayer.TimerTick -= OnSessionManagerTimerTick;
        _wellnessExercisePlayer.BreathingCircleAnimationTimerTick -= OnBreathingCircleAnimationTimerTick;
        _wellnessExercisePlayer.TimerFinished -= OnSessionManagerTimerFinished;
        */

        FinishSession();
    }

    private void FinishSession()
    {
        // create the request to save the session
        /*
        var request = new CreateWellnessSessionEntryRequest
        (
            StartDate: _startDate,
            Duration: _sessionConfiguration.TargetDuration,
            CycleCount: // TODO: track cycles in player and get actual cycle count
            Exercise: _sessionConfiguration.Exercise
        );
        */

        // SessionEnded?.Invoke(this, request);
    }

    #endregion
}