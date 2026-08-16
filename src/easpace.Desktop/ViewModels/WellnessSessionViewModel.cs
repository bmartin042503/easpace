// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;
using easpace.Desktop.Services;

namespace easpace.Desktop.ViewModels;

public partial class WellnessSessionViewModel : ViewModelBase
{
    #region Fields

    private DispatcherTimer? _timer;
    private TimeSpan _timeLeft;
    private TimeSpan _elapsedTime;
    private DateTimeOffset _startDate;

    private WellnessSessionConfiguration _sessionConfiguration;

    private int _currentPhaseIndex;
    private int _currentPhaseElapsedSeconds;
    private int _cycleCount;

    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _instructionText = string.Empty;
    [ObservableProperty] private string _phaseSecondsText = string.Empty;
    [ObservableProperty] private string _timerText = "00:00";
    [ObservableProperty] private double _breathingCircleSize = 64;

    [ObservableProperty]
    private string _timerToggleButtonText = LocalizationService.GetString("Wellness.Button.PauseSession");

    private DispatcherTimer? _breathingAnimationTimer;
    private double _breathingCircleStartSize;
    private double _breathingCircleTargetSize;
    private TimeSpan _breathingAnimationDuration;
    private TimeSpan _breathingAnimationElapsed;

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the wellness session has ended or is manually stopped.
    /// </summary>
    public event EventHandler<WellnessSession>? SessionEnded;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether the current session is a breathing exercise.
    /// </summary>
    public bool IsBreathing { get; set; }

    /// <summary>
    /// Gets or sets the list of breathing phases for the active technique.
    /// </summary>
    public List<BreathingPhase> Phases { get; set; } = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessSessionViewModel"/> class.
    /// </summary>
    /// <param name="sessionConfiguration">The configuration parameters for the current session.</param>
    public WellnessSessionViewModel(WellnessSessionConfiguration sessionConfiguration)
    {
        _sessionConfiguration = sessionConfiguration;
        _elapsedTime = TimeSpan.Zero;

        // initialize the main session timer
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;

        // setup initial timer text display based on user configuration
        if (_sessionConfiguration.IsTimerSet)
        {
            _timeLeft = _sessionConfiguration.TargetDuration;
            UpdateTimerText(_timeLeft);
        }
        else
        {
            UpdateTimerText(_elapsedTime);
        }

        // configure specific session type properties
        switch (_sessionConfiguration.SessionType)
        {
            case WellnessSessionType.Breathing:
                _breathingAnimationTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                _breathingAnimationTimer.Tick += OnBreathingAnimationTimerTick;

                InitializeBreathingPhase();
                IsBreathing = true;
                break;

            case WellnessSessionType.Meditation:
                InstructionText = LocalizationService.GetString("Wellness.Instruction.Meditation");
                IsBreathing = false;
                break;
        }

        // start the session
        _timer.Start();
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
            // resume the main timer
            _timer?.Start();

            // resume animation if it's currently transitioning between sizes
            if (Math.Abs(_breathingCircleStartSize - _breathingCircleTargetSize) > 0.1)
            {
                _breathingAnimationTimer?.Start();
            }

            IsPaused = false;
            TimerToggleButtonText = LocalizationService.GetString("Wellness.Button.PauseSession");
        }
        else
        {
            // pause both timers
            _timer?.Stop();
            _breathingAnimationTimer?.Stop();

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
        FinishSession();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Prepares the initial state for the breathing phase sequence.
    /// </summary>
    private void InitializeBreathingPhase()
    {
        // verify that the configured technique contains valid phases
        if (_sessionConfiguration.BreathingTechniqueConfiguration?.BreathingTechnique?.Phases.Count > 0)
        {
            _currentPhaseIndex = 0;
            _currentPhaseElapsedSeconds = 0;
            Phases = _sessionConfiguration.BreathingTechniqueConfiguration.BreathingTechnique.Phases;

            UpdateBreathingInstruction();
            UpdatePhaseAnimation(Phases[_currentPhaseIndex]);
        }
    }

    /// <summary>
    /// Handles the main timer tick every second, managing elapsed time, countdowns, and phase tracking.
    /// </summary>
    private void OnTimerTick(object? sender, EventArgs e)
    {
        // increment total elapsed time
        _elapsedTime = _elapsedTime.Add(TimeSpan.FromSeconds(1));

        if (_sessionConfiguration.SessionType == WellnessSessionType.Breathing)
        {
            ProcessBreathingPhase();
        }

        // evaluate timer limits if a specific duration was set
        if (_sessionConfiguration.IsTimerSet)
        {
            _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
            UpdateTimerText(_timeLeft);

            if (_timeLeft.TotalSeconds <= 0)
            {
                FinishSession();
            }
        }
        else
        {
            UpdateTimerText(_elapsedTime);
        }
    }

    /// <summary>
    /// Processes the current breathing phase state and progresses to the next phase or cycle when necessary.
    /// </summary>
    private void ProcessBreathingPhase()
    {
        var phases = _sessionConfiguration.BreathingTechniqueConfiguration?.BreathingTechnique?.Phases;
        if (phases == null || phases.Count == 0) return;

        _currentPhaseElapsedSeconds++;

        var currentPhase = phases[_currentPhaseIndex];

        // check if the current phase duration has elapsed
        if (_currentPhaseElapsedSeconds >= currentPhase.DurationSeconds)
        {
            _currentPhaseIndex++;
            _currentPhaseElapsedSeconds = 0;

            // check if a full breathing cycle is completed
            if (_currentPhaseIndex >= phases.Count)
            {
                _currentPhaseIndex = 0;
                _cycleCount++;
            }

            var newPhase = phases[_currentPhaseIndex];
            UpdatePhaseAnimation(newPhase);
        }

        UpdateBreathingInstruction();
    }

    /// <summary>
    /// Configures the animation properties for the specified breathing phase.
    /// </summary>
    /// <param name="phase">The breathing phase to animate.</param>
    private void UpdatePhaseAnimation(BreathingPhase phase)
    {
        _breathingCircleStartSize = BreathingCircleSize;

        // determine the target circle size based on the phase action
        _breathingCircleTargetSize = phase.Type switch
        {
            BreathingPhaseType.Inhale => 128.0,
            BreathingPhaseType.Exhale => 32.0,
            _ => BreathingCircleSize
        };

        _breathingAnimationDuration = TimeSpan.FromSeconds(phase.DurationSeconds);
        _breathingAnimationElapsed = TimeSpan.Zero;

        // start the animation timer if there is a size transition and the session is active
        if (!IsPaused && Math.Abs(_breathingCircleStartSize - _breathingCircleTargetSize) > 0.1)
        {
            _breathingAnimationTimer?.Start();
        }
    }

    /// <summary>
    /// Executes the rendering logic for the breathing circle animation at approximately 60 FPS.
    /// </summary>
    private void OnBreathingAnimationTimerTick(object? sender, EventArgs e)
    {
        if (_breathingAnimationTimer == null || _breathingAnimationDuration.TotalMilliseconds <= 0) return;

        _breathingAnimationElapsed += _breathingAnimationTimer.Interval;

        var progress = _breathingAnimationElapsed.TotalMilliseconds / _breathingAnimationDuration.TotalMilliseconds;

        // clamp progress to 100% and stop the animation timer
        if (progress >= 1.0)
        {
            progress = 1.0;
            _breathingAnimationTimer.Stop();
        }

        // apply a sine easing function for a smoother, more natural breathing effect
        var easedProgress = -(Math.Cos(Math.PI * progress) - 1) / 2;

        BreathingCircleSize = _breathingCircleStartSize +
                              (_breathingCircleTargetSize - _breathingCircleStartSize) * easedProgress;
    }

    /// <summary>
    /// Concludes the session, stops all timers, aggregates session data, and triggers the end event.
    /// </summary>
    private void FinishSession()
    {
        // stop and detach timers to prevent memory leaks
        _timer?.Stop();
        _breathingAnimationTimer?.Stop();

        if (_timer != null) _timer.Tick -= OnTimerTick;
        if (_breathingAnimationTimer != null) _breathingAnimationTimer.Tick -= OnBreathingAnimationTimerTick;

        BreathingTechnique? techniqueToSave = null;

        // populate the completed technique details if applicable
        if (_sessionConfiguration.BreathingTechniqueConfiguration != null)
        {
            var configTech = _sessionConfiguration.BreathingTechniqueConfiguration.BreathingTechnique;
            if (configTech != null)
            {
                techniqueToSave = new BreathingTechnique
                {
                    Id = configTech.Id,
                    Name = configTech.Name,
                    Description = configTech.Description,
                    Phases = configTech.Phases,
                    Cycles = _sessionConfiguration.BreathingTechniqueConfiguration.Cycles
                };
            }
        }

        // create the final session object
        var session = new WellnessSession
        {
            Id = Guid.NewGuid(),
            StartDate = _startDate,
            TargetDuration = _sessionConfiguration.TargetDuration,
            ActualDuration = _elapsedTime,
            Type = _sessionConfiguration.SessionType,
            BreathingTechnique = techniqueToSave,
            ActualBreathingCycles = _cycleCount
        };

        SessionEnded?.Invoke(this, session);
    }

    /// <summary>
    /// Updates the instructional text and phase countdown displayed to the user based on the active phase.
    /// </summary>
    private void UpdateBreathingInstruction()
    {
        if (_sessionConfiguration.SessionType != WellnessSessionType.Breathing) return;

        var phases = _sessionConfiguration.BreathingTechniqueConfiguration?.BreathingTechnique?.Phases;
        if (phases == null || phases.Count == 0) return;

        var currentPhase = phases[_currentPhaseIndex];

        var remainingPhaseSeconds = currentPhase.DurationSeconds - _currentPhaseElapsedSeconds;

        // map the current phase type to the corresponding localized instruction string
        var instructionText = currentPhase.Type switch
        {
            BreathingPhaseType.Inhale => LocalizationService.GetString("Wellness.Instruction.BreatheIn"),
            BreathingPhaseType.HoldIn => LocalizationService.GetString("Wellness.Instruction.Hold"),
            BreathingPhaseType.Exhale => LocalizationService.GetString("Wellness.Instruction.BreatheOut"),
            BreathingPhaseType.HoldOut => LocalizationService.GetString("Wellness.Instruction.Hold"),
            _ => string.Empty
        };

        InstructionText = instructionText;
        PhaseSecondsText = remainingPhaseSeconds.ToString();
    }

    /// <summary>
    /// Formats and updates the main timer display text.
    /// </summary>
    /// <param name="time">The time span to format.</param>
    private void UpdateTimerText(TimeSpan time)
    {
        // display hours if the session exceeds 60 minutes, otherwise show minutes and seconds
        TimerText = time.ToString(time.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
    }

    #endregion
}