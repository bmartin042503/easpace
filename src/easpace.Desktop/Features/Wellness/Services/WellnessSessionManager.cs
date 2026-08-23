// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services;

namespace easpace.Desktop.Features.Wellness.Services;

public class WellnessSessionManager : IWellnessSessionManager
{
    private DispatcherTimer? _timer;
    private TimeSpan _timeLeft;
    public TimeSpan ElapsedTime { get; private set; }
    
    private int _currentPhaseIndex;
    private int _currentPhaseElapsedSeconds;

    private DispatcherTimer? _breathingAnimationTimer;
    private double _breathingCircleStartSize;
    private double _breathingCircleTargetSize;
    private TimeSpan _breathingAnimationDuration;
    private TimeSpan _breathingAnimationElapsed;

    private double _breathingCircleSize = 64;
    
    private WellnessSessionConfiguration _sessionConfiguration;
    
    private List<BreathingPhase> _phases = [];

    private string _instructionText = string.Empty;
    private string _phaseSecondsText = string.Empty;
    
    public bool IsPaused { get; private set; }

    public event EventHandler? TimerFinished;
    public event EventHandler<SessionTexts>? TimerTick;
    public event EventHandler<double>? BreathingCircleAnimationTimerTick;

    private int _meditationInstructElapsedSeconds;
    private int _meditationInstructSwitchIntervalSeconds = 3 * 60;

    private List<string> _meditationInstructTexts = [
        LocalizationService.GetString("Wellness.Instruction1.Meditation"),
        LocalizationService.GetString("Wellness.Instruction2.Meditation"),
        LocalizationService.GetString("Wellness.Instruction3.Meditation"),
        LocalizationService.GetString("Wellness.Instruction4.Meditation"),
        LocalizationService.GetString("Wellness.Instruction5.Meditation"),
        LocalizationService.GetString("Wellness.Instruction6.Meditation")
    ];

    public WellnessSessionManager(
        WellnessSessionConfiguration sessionConfiguration)
    {
        _sessionConfiguration = sessionConfiguration;
    }

    public void StartSession()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer?.Start();

        if (_sessionConfiguration.SessionType == WellnessSessionType.Breathing)
        {
            StartBreathingTechnique();
        }
        else
        {
            var randomIndex = new Random().Next(0, _meditationInstructTexts.Count - 1);
            _instructionText = _meditationInstructTexts[randomIndex];
        }
        
        if (_sessionConfiguration.IsTimerSet)
        {
            _timeLeft = _sessionConfiguration.TargetDuration ?? TimeSpan.Zero;
        }
    }
    
    public void PauseSession()
    {
        _timer?.Stop();
        _breathingAnimationTimer?.Stop();
        IsPaused = true;
    }

    public void ResumeSession()
    {
        _timer?.Start();

        // resume animation if it's currently transitioning between sizes
        if (Math.Abs(_breathingCircleStartSize - _breathingCircleTargetSize) > 0.1)
        {
            _breathingAnimationTimer?.Start();
        }

        IsPaused = false;
    }

    public void StopSession()
    {
        _timer?.Stop();
        _breathingAnimationTimer?.Stop();
        
        _timer?.Tick -= OnTimerTick;
        _breathingAnimationTimer?.Tick -= OnBreathingAnimationTimerTick;
    }
    
    private void StartBreathingTechnique()
    {
        // verify that the configured technique contains valid phases
        if (_sessionConfiguration.BreathingTechniqueConfiguration?.BreathingTechnique?.Phases.Count > 0)
        {
            _breathingAnimationTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _breathingAnimationTimer.Tick += OnBreathingAnimationTimerTick;
            
            _currentPhaseIndex = 0;
            _currentPhaseElapsedSeconds = 0;
            
            _phases = _sessionConfiguration.BreathingTechniqueConfiguration.BreathingTechnique.Phases
                .OrderBy(p => p.Order)
                .ToList();

            UpdateBreathingInstruction();
            UpdatePhaseAnimation(_phases[_currentPhaseIndex]);
        }
    }
    
    private void ProcessBreathingPhase()
    {
        if (_phases.Count == 0) return;

        _currentPhaseElapsedSeconds++;

        var currentPhase = _phases[_currentPhaseIndex];

        // check if the current phase duration has elapsed
        if (_currentPhaseElapsedSeconds >= currentPhase.DurationSeconds)
        {
            _currentPhaseIndex++;
            _currentPhaseElapsedSeconds = 0;

            // check if a full breathing cycle is completed
            if (_currentPhaseIndex >= _phases.Count)
            {
                _currentPhaseIndex = 0;
            }

            var newPhase = _phases[_currentPhaseIndex];
            UpdatePhaseAnimation(newPhase);
        }

        UpdateBreathingInstruction();
    }
    
    private void UpdatePhaseAnimation(BreathingPhase phase)
    {
        _breathingCircleStartSize = _breathingCircleSize;

        // determine the target circle size based on the phase action
        _breathingCircleTargetSize = phase.Type switch
        {
            BreathingPhaseType.Inhale => 128.0,
            BreathingPhaseType.Exhale => 32.0,
            _ => _breathingCircleSize
        };

        _breathingAnimationDuration = TimeSpan.FromSeconds(phase.DurationSeconds);
        _breathingAnimationElapsed = TimeSpan.Zero;

        // start the animation timer if there is a size transition and the session is active
        if (!IsPaused && Math.Abs(_breathingCircleStartSize - _breathingCircleTargetSize) > 0.1)
        {
            _breathingAnimationTimer?.Start();
        }
    }
    
    private void UpdateBreathingInstruction()
    {
        if (_sessionConfiguration.SessionType != WellnessSessionType.Breathing) return;

        if (_phases.Count == 0) return;

        var currentPhase = _phases[_currentPhaseIndex];

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

        _instructionText = instructionText;
        _phaseSecondsText = remainingPhaseSeconds.ToString();
    }
    
    private void OnTimerTick(object? sender, EventArgs e)
    {
        // increment total elapsed time
        ElapsedTime = ElapsedTime.Add(TimeSpan.FromSeconds(1));

        if (_sessionConfiguration.SessionType == WellnessSessionType.Breathing)
        {
            ProcessBreathingPhase();
        }
        else if (_sessionConfiguration.SessionType == WellnessSessionType.Meditation)
        {
            if (_meditationInstructElapsedSeconds == _meditationInstructSwitchIntervalSeconds)
            {
                var randomIndex = new Random().Next(0, _meditationInstructTexts.Count - 1);
                _instructionText = _meditationInstructTexts[randomIndex];
                
                _meditationInstructElapsedSeconds = 0;
            }
            
            _meditationInstructElapsedSeconds++;
        }

        string timerText;

        // evaluate timer limits if a specific duration was set
        if (_sessionConfiguration.IsTimerSet)
        {
            _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
            timerText = GetTimerText(_timeLeft);

            if (_timeLeft.TotalSeconds <= 0)
            {
                TimerFinished?.Invoke(this, EventArgs.Empty);
                StopSession();
            }
        }
        else
        {
            timerText = GetTimerText(ElapsedTime);
        }
        
        TimerTick?.Invoke(this, new SessionTexts(timerText, _instructionText, _phaseSecondsText));
    }
    
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

        _breathingCircleSize = _breathingCircleStartSize +
                              (_breathingCircleTargetSize - _breathingCircleStartSize) * easedProgress;
        
        BreathingCircleAnimationTimerTick?.Invoke(this, _breathingCircleSize);
    }
    
    public string GetTimerText(TimeSpan time)
    {
        // display hours if the session exceeds 60 minutes, otherwise show minutes and seconds
        return time.ToString(time.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
    }
}