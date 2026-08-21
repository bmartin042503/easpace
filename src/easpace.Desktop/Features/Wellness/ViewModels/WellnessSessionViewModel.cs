// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

public partial class WellnessSessionViewModel : ViewModelBase
{
    #region Fields
    
    private DateTimeOffset _startDate;
    
    private WellnessSessionConfiguration _sessionConfiguration;
    
    private IWellnessSessionManager _wellnessSessionManager;

    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _instructionText = string.Empty;
    [ObservableProperty] private string _phaseSecondsText = string.Empty;
    [ObservableProperty] private string _timerText = "00:00";
    [ObservableProperty] private double _breathingCircleSize = 64;

    [ObservableProperty]
    private string _timerToggleButtonText = LocalizationService.GetString("Wellness.Button.PauseSession");
    
    public bool IsBreathing { get; set; }

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
    /// <param name="sessionConfiguration">The configuration parameters for the current session.</param>
    public WellnessSessionViewModel(WellnessSessionConfiguration sessionConfiguration)
    {
        _sessionConfiguration = sessionConfiguration;
        
        _wellnessSessionManager = new WellnessSessionManager(sessionConfiguration);

        _wellnessSessionManager.TimerTick += OnSessionManagerTimerTick;
        _wellnessSessionManager.BreathingCircleAnimationTimerTick += OnBreathingCircleAnimationTimerTick;
        _wellnessSessionManager.TimerFinished += OnSessionManagerTimerFinished;

        // setup initial timer text display based on user configuration
        if (_sessionConfiguration is { IsTimerSet: true, TargetDuration: not null })
        {
            TimerText = _wellnessSessionManager.GetTimerText(_sessionConfiguration.TargetDuration.Value);
        }

        if (_sessionConfiguration.SessionType == WellnessSessionType.Breathing)
        {
            IsBreathing = true;
            var firstPhase = _sessionConfiguration.BreathingTechniqueConfiguration?.BreathingTechnique?.Phases.ToList()[0];
            InstructionText = firstPhase?.Type switch
            {
                BreathingPhaseType.Inhale => LocalizationService.GetString("Wellness.Instruction.BreatheIn"),
                BreathingPhaseType.HoldIn => LocalizationService.GetString("Wellness.Instruction.Hold"),
                BreathingPhaseType.Exhale => LocalizationService.GetString("Wellness.Instruction.BreatheOut"),
                BreathingPhaseType.HoldOut => LocalizationService.GetString("Wellness.Instruction.Hold"),
                _ => LocalizationService.GetString("Wellness.Instruction.BreatheIn")
            };
        }
        else
        {
            IsBreathing = false;
        }
        
        // configure specific session type properties
        _wellnessSessionManager.StartSession();
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
            _wellnessSessionManager.ResumeSession();

            IsPaused = false;
            TimerToggleButtonText = LocalizationService.GetString("Wellness.Button.PauseSession");
        }
        else
        {
            _wellnessSessionManager.PauseSession();

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
        _wellnessSessionManager.StopSession();

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
        _wellnessSessionManager.TimerTick -= OnSessionManagerTimerTick;
        _wellnessSessionManager.BreathingCircleAnimationTimerTick -= OnBreathingCircleAnimationTimerTick;
        _wellnessSessionManager.TimerFinished -= OnSessionManagerTimerFinished;

        FinishSession();
    }

    private void FinishSession()
    {
        // create the request to save the session
        var request = new CreateWellnessSessionEntryRequest
        (
            StartDate: _startDate,
            TargetDuration: _sessionConfiguration.TargetDuration,
            ActualDuration: _wellnessSessionManager.ElapsedTime,
            SessionType: _sessionConfiguration.SessionType,
            BreathingTechnique: _sessionConfiguration.BreathingTechniqueConfiguration?.BreathingTechnique
        );
        
        SessionEnded?.Invoke(this, request);
    }

    #endregion
}