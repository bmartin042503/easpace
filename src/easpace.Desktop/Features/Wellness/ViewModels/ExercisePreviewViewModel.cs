// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

/// <summary>
/// Plays the steps of the edited exercise in a loop, with the same engine as a real session.
/// </summary>
internal sealed partial class ExercisePreviewViewModel : ViewModelBase, IDisposable
{
    private IReadOnlyList<ExerciseStep> _steps = [];
    private IWellnessSessionManager? _sessionManager;

    // the timers of a session manager only exist once it's started
    private bool _isSessionStarted;
    private bool _isDisposed;

    /// <summary>
    /// Gets the current position of the preview, or <c>null</c> when there are no steps to play.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSteps))]
    [NotifyPropertyChangedFor(nameof(ShowBreathingCircle))]
    [NotifyPropertyChangedFor(nameof(ShowCountdown))]
    [NotifyPropertyChangedFor(nameof(InstructionText))]
    [NotifyPropertyChangedFor(nameof(StepSecondsText))]
    [NotifyPropertyChangedFor(nameof(PositionText))]
    [NotifyCanExecuteChangedFor(nameof(TogglePlayPauseCommand))]
    [NotifyCanExecuteChangedFor(nameof(StepForwardCommand))]
    [NotifyCanExecuteChangedFor(nameof(StepBackwardCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    private SessionProgress? _progress;

    /// <summary>
    /// Gets whether the preview plays. It stays set while there are no steps, so the preview continues once there are.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TogglePlayPauseText))]
    private bool _isPlaying;

    [ObservableProperty] private double _circleSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowBreathingCircle))]
    [NotifyPropertyChangedFor(nameof(ShowCountdown))]
    private WellnessSessionType _type;

    public bool HasSteps => Progress is not null;

    /// <summary>
    /// Gets whether the breathing circle is shown, which only breathing steps with a phase have, as in a session.
    /// </summary>
    public bool ShowBreathingCircle => Type == WellnessSessionType.Breathing && Progress?.Phase is not null;

    /// <summary>
    /// Gets whether the seconds of the step are shown on their own, as there is no circle to show them in.
    /// </summary>
    public bool ShowCountdown => HasSteps && !ShowBreathingCircle;

    // read from the latest steps, as a change of the texts only doesn't rebuild the session
    public string InstructionText =>
        Progress is { } progress && progress.StepIndex < _steps.Count ? _steps[progress.StepIndex].Text : string.Empty;

    public string StepSecondsText => Progress?.StepSecondsText ?? string.Empty;

    public string PositionText => Progress is { } progress
        ? string.Format(LocalizationService.GetString("Wellness.Preview.Position"),
            progress.StepIndex + 1, progress.StepCount, progress.CycleIndex + 1)
        : string.Empty;

    public string TogglePlayPauseText => LocalizationService.GetString(IsPlaying ? "Wellness.Preview.Pause" : "Wellness.Preview.Play");

    /// <summary>
    /// Shows new steps. The position is kept, moved to the last step when it no longer exists, and the preview keeps
    /// playing or stays paused. When only texts changed, the current step also keeps its remaining time.
    /// </summary>
    /// <param name="steps">The steps of one cycle; the preview repeats them endlessly.</param>
    /// <param name="type">The session type of the exercise, which decides whether the breathing circle is shown.</param>
    public void Load(IReadOnlyList<ExerciseStep> steps, WellnessSessionType type)
    {
        if (_isDisposed) return;

        var previousSteps = _steps;
        _steps = steps;
        Type = type;

        if (steps.Count == 0)
        {
            StopSession();
            Progress = null;
            return;
        }

        if (_sessionManager is not null && HaveSameTiming(previousSteps, steps))
        {
            OnPropertyChanged(nameof(InstructionText));
            return;
        }

        var position = Progress;
        StopSession();

        _sessionManager = new WellnessSessionManager(
            new WellnessSessionConfiguration(type, null, string.Empty, steps, null, true));

        if (position is not null)
        {
            var jumps = position.CycleIndex * steps.Count + Math.Min(position.StepIndex, steps.Count - 1);
            for (var i = 0; i < jumps; i++)
            {
                _sessionManager.StepForward();
            }
        }

        _sessionManager.ProgressChanged += OnSessionProgressChanged;
        _sessionManager.BreathingCircleAnimationTimerTick += OnBreathingCircleAnimationTimerTick;

        CircleSize = _sessionManager.BreathingCircleSize;
        Progress = _sessionManager.Progress;

        if (IsPlaying) StartOrResumeSession();
    }

    /// <summary>
    /// Stops the preview and removes its steps.
    /// </summary>
    public void Clear()
    {
        StopSession();
        _steps = [];
        Progress = null;
        IsPlaying = false;
    }

    /// <summary>
    /// Pauses the preview, e.g. when it's no longer visible.
    /// </summary>
    public void Pause()
    {
        if (_isSessionStarted) _sessionManager?.PauseSession();

        IsPlaying = false;
    }

    /// <summary>
    /// Stops the timers for good. The preview can't be used afterwards.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        Clear();
        _isDisposed = true;
    }

    [RelayCommand(CanExecute = nameof(HasSteps))]
    private void TogglePlayPause()
    {
        if (IsPlaying)
        {
            Pause();
            return;
        }

        IsPlaying = true;
        StartOrResumeSession();
    }

    [RelayCommand(CanExecute = nameof(HasSteps))]
    private void StepForward() => _sessionManager?.StepForward();

    [RelayCommand(CanExecute = nameof(CanStepBackward))]
    private void StepBackward() => _sessionManager?.StepBackward();

    /// <summary>
    /// Returns to the first step of the first cycle and keeps playing or stays paused.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSteps))]
    public void Restart() => _sessionManager?.Restart();

    private bool CanStepBackward() => Progress is { } progress && (progress.StepIndex > 0 || progress.CycleIndex > 0);

    private void StartOrResumeSession()
    {
        if (_sessionManager is null) return;

        if (_isSessionStarted)
        {
            _sessionManager.ResumeSession();
            return;
        }

        _sessionManager.StartSession();
        _isSessionStarted = true;
    }

    private void StopSession()
    {
        if (_sessionManager is null) return;

        _sessionManager.StopSession();
        _sessionManager.ProgressChanged -= OnSessionProgressChanged;
        _sessionManager.BreathingCircleAnimationTimerTick -= OnBreathingCircleAnimationTimerTick;
        _sessionManager = null;
        _isSessionStarted = false;
    }

    private static bool HaveSameTiming(IReadOnlyList<ExerciseStep> first, IReadOnlyList<ExerciseStep> second) =>
        first.Count == second.Count
        && first.Zip(second).All(s => s.First.DurationSeconds == s.Second.DurationSeconds && s.First.Phase == s.Second.Phase);

    private void OnSessionProgressChanged(object? sender, SessionProgress progress) => Progress = progress;

    private void OnBreathingCircleAnimationTimerTick(object? sender, double size) => CircleSize = size;
}
