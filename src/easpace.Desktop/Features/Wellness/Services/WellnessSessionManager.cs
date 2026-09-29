// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Features.Wellness.Services;

internal class WellnessSessionManager : IWellnessSessionManager
{
    private const double BreathingCircleMinSize = 96;
    private const double BreathingCircleMaxSize = 192;

    private DispatcherTimer? _timer;
    private TimeSpan _timeLeft;
    public TimeSpan ElapsedTime { get; private set; }

    // drives breathing sessions; null for meditation, which rotates random instructions instead
    private readonly ExerciseSequence? _sequence;

    private DispatcherTimer? _breathingAnimationTimer;
    private double _breathingCircleStartSize;
    private double _breathingCircleTargetSize;
    private TimeSpan _breathingAnimationDuration;
    private TimeSpan _breathingAnimationElapsed;

    private double _breathingCircleSize = BreathingCircleMinSize;

    private WellnessSessionConfiguration _sessionConfiguration;

    private string _instructionText = string.Empty;

    public bool IsPaused { get; private set; }

    public SessionProgress Progress => CreateProgress();

    public event EventHandler? TimerFinished;
    public event EventHandler<SessionProgress>? ProgressChanged;
    public event EventHandler<double>? BreathingCircleAnimationTimerTick;

    private int _meditationInstructElapsedSeconds;
    private const int MeditationInstructSwitchIntervalSeconds = 2 * 60;

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
        _sequence = CreateBreathingSequence(sessionConfiguration);
        _timeLeft = _sessionConfiguration.TargetDuration ?? TimeSpan.Zero;
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
            var randomIndex = Random.Shared.Next(_meditationInstructTexts.Count);
            _instructionText = _meditationInstructTexts[randomIndex];
        }

        ProgressChanged?.Invoke(this, Progress);
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
    
    /// <summary>
    /// Builds the instruction sequence of a breathing session from the phases of its technique.
    /// Returns <c>null</c> for other session types and for techniques without phases.
    /// </summary>
    private static ExerciseSequence? CreateBreathingSequence(WellnessSessionConfiguration configuration)
    {
        var techniqueConfiguration = configuration.BreathingTechniqueConfiguration;

        if (configuration.SessionType != WellnessSessionType.Breathing
            || techniqueConfiguration?.BreathingTechnique is not { Phases.Count: > 0 } technique)
        {
            return null;
        }

        var steps = technique.Phases
            .OrderBy(p => p.Order)
            .Select(p => ExerciseStep.Create(null, p.DurationSeconds, p.Type))
            .ToList();

        return new ExerciseSequence(steps, techniqueConfiguration.Cycles);
    }

    private void StartBreathingTechnique()
    {
        // a technique without valid phases has nothing to animate
        if (_sequence is null) return;

        _breathingAnimationTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _breathingAnimationTimer.Tick += OnBreathingAnimationTimerTick;

        UpdatePhaseAnimation(_sequence.CurrentStep);
    }

    private void ProcessBreathingPhase()
    {
        if (_sequence is null) return;

        // animate the next phase once the current one has elapsed, unless the whole sequence is completed
        if (_sequence.Tick() && !_sequence.IsCompleted)
        {
            UpdatePhaseAnimation(_sequence.CurrentStep);
        }
    }

    private void UpdatePhaseAnimation(ExerciseStep step)
    {
        _breathingCircleStartSize = _breathingCircleSize;

        // the target size depends only on the phase, so it doesn't drift with the animation history
        _breathingCircleTargetSize = step.Phase switch
        {
            BreathingPhaseType.Inhale or BreathingPhaseType.HoldIn => BreathingCircleMaxSize,
            BreathingPhaseType.Exhale or BreathingPhaseType.HoldOut => BreathingCircleMinSize,
            _ => _breathingCircleSize
        };

        _breathingAnimationDuration = TimeSpan.FromSeconds(step.DurationSeconds);
        _breathingAnimationElapsed = TimeSpan.Zero;

        // start the animation timer if there is a size transition and the session is active
        if (!IsPaused && Math.Abs(_breathingCircleStartSize - _breathingCircleTargetSize) > 0.1)
        {
            _breathingAnimationTimer?.Start();
        }
    }

    private SessionProgress CreateProgress()
    {
        if (_sequence is null)
        {
            return new SessionProgress(
                TimerText: GetTimerText(_timeLeft),
                InstructionText: _instructionText,
                StepSecondsText: string.Empty,
                Phase: null,
                StepIndex: 0,
                StepCount: 0,
                CycleIndex: 0,
                Cycles: null);
        }

        // show the remaining time of a finite sequence and the elapsed time of an endless one
        var timerSeconds = _sequence.TotalSeconds is { } totalSeconds
            ? totalSeconds - _sequence.ElapsedSeconds
            : _sequence.ElapsedSeconds;

        return new SessionProgress(
            TimerText: GetTimerText(TimeSpan.FromSeconds(timerSeconds)),
            InstructionText: _sequence.CurrentStep.Text,
            StepSecondsText: _sequence.StepRemainingSeconds.ToString(),
            Phase: _sequence.CurrentStep.Phase,
            StepIndex: _sequence.StepIndex,
            StepCount: _sequence.Steps.Count,
            CycleIndex: _sequence.CycleIndex,
            Cycles: _sequence.Cycles);
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
            if (_meditationInstructElapsedSeconds == MeditationInstructSwitchIntervalSeconds)
            {
                var randomIndex = Random.Shared.Next(_meditationInstructTexts.Count);
                _instructionText = _meditationInstructTexts[randomIndex];
                
                _meditationInstructElapsedSeconds = 0;
            }
            
            _meditationInstructElapsedSeconds++;
        }

        bool isFinished;

        if (_sequence is not null)
        {
            isFinished = _sequence.IsCompleted;
        }
        else
        {
            // sessions without an instruction sequence count down the target duration
            _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
            isFinished = _timeLeft.TotalSeconds <= 0;
        }

        if (isFinished)
        {
            TimerFinished?.Invoke(this, EventArgs.Empty);
            StopSession();
        }

        ProgressChanged?.Invoke(this, Progress);
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