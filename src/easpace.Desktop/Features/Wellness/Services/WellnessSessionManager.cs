// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia.Threading;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;

namespace easpace.Desktop.Features.Wellness.Services;

internal class WellnessSessionManager : IWellnessSessionManager
{
    private const double BreathingCircleMinSize = 96;
    private const double BreathingCircleMaxSize = 192;

    private DispatcherTimer? _timer;
    public TimeSpan ElapsedTime { get; private set; }

    private readonly ExerciseSequence _sequence;

    private DispatcherTimer? _breathingAnimationTimer;
    private double _breathingCircleStartSize;
    private double _breathingCircleTargetSize;
    private TimeSpan _breathingAnimationDuration;
    private TimeSpan _breathingAnimationElapsed;

    private double _breathingCircleSize = BreathingCircleMinSize;

    public bool IsPaused { get; private set; }

    public SessionProgress Progress => CreateProgress();

    public int CompletedCycles => _sequence.CompletedCycles;

    public double BreathingCircleSize => _breathingCircleSize;

    public event EventHandler? TimerFinished;
    public event EventHandler<SessionProgress>? ProgressChanged;
    public event EventHandler<double>? BreathingCircleAnimationTimerTick;

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessSessionManager"/> class.
    /// </summary>
    /// <param name="sessionConfiguration">The steps and cycles of the session to run.</param>
    /// <exception cref="ArgumentException">Thrown when the configuration has no steps or a step without duration.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when fewer than one cycle is configured.</exception>
    public WellnessSessionManager(
        WellnessSessionConfiguration sessionConfiguration)
    {
        _sequence = new ExerciseSequence(sessionConfiguration.Steps, sessionConfiguration.Cycles);
    }

    public void StartSession()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer?.Start();

        _breathingAnimationTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _breathingAnimationTimer.Tick += OnBreathingAnimationTimerTick;

        UpdatePhaseAnimation(_sequence.CurrentStep);

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
    
    public bool StepForward()
    {
        if (!_sequence.MoveNext()) return false;

        if (_sequence.IsCompleted)
        {
            TimerFinished?.Invoke(this, EventArgs.Empty);
            StopSession();
            ProgressChanged?.Invoke(this, Progress);
            return true;
        }

        OnStepJumped();
        return true;
    }

    public bool StepBackward()
    {
        if (!_sequence.MovePrevious()) return false;

        OnStepJumped();
        return true;
    }

    public void Restart()
    {
        _sequence.Reset();
        ElapsedTime = TimeSpan.Zero;

        OnStepJumped();
    }

    /// <summary>
    /// Shows the new step from its start: the circle snaps to where the phase begins and the session timer starts a
    /// full second, but only if it was running, so a paused session stays paused.
    /// </summary>
    private void OnStepJumped()
    {
        _breathingAnimationTimer?.Stop();

        if (_timer is { IsEnabled: true })
        {
            _timer.Stop();
            _timer.Start();
        }

        var step = _sequence.CurrentStep;

        // without a phase the circle is hidden, so it keeps its size
        _breathingCircleSize = step.Phase switch
        {
            BreathingPhaseType.Inhale or BreathingPhaseType.HoldOut => BreathingCircleMinSize,
            BreathingPhaseType.Exhale or BreathingPhaseType.HoldIn => BreathingCircleMaxSize,
            _ => _breathingCircleSize
        };
        BreathingCircleAnimationTimerTick?.Invoke(this, _breathingCircleSize);

        UpdatePhaseAnimation(step);

        ProgressChanged?.Invoke(this, Progress);
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

        // animate the next phase once the current one has elapsed, unless the whole sequence is completed
        if (_sequence.Tick() && !_sequence.IsCompleted)
        {
            UpdatePhaseAnimation(_sequence.CurrentStep);
        }

        if (_sequence.IsCompleted)
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