// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

/// <summary>
/// Controls exercise preview playback and provides instruction and breathing animation data.
/// </summary>
internal partial class WellnessExercisePreviewViewModel : ViewModelBase, IDisposable
{
    private const double MinBreathingCircleSize = 96;
    private const double MaxBreathingCircleSize = 192;

    private readonly IWellnessExercisePlayer _player;
    private readonly TimeProvider _timeProvider;
    private readonly DispatcherTimer _timer;

    private WellnessExercise? _exercise;
    private int _cycleCount;
    private long _lastTimestamp;
    private bool _isLoading;
    private bool _isDisposed;

    public WellnessExercisePlaybackState State => _player.State;
    public bool IsPlaying => State == WellnessExercisePlaybackState.Playing;
    public bool IsPaused => State == WellnessExercisePlaybackState.Paused;

    public int CurrentCycle => _player.CurrentCycle;
    public int TotalCycles => _player.TotalCycles;
    public int CurrentInstructionIndex => _player.CurrentInstructionIndex;
    public WellnessExerciseInstruction? CurrentInstruction => _player.CurrentInstruction;

    public TimeSpan InstructionElapsed => _player.InstructionElapsed;
    public TimeSpan InstructionRemaining => _player.InstructionRemaining;
    public string InstructionText => CurrentInstruction?.Text ?? string.Empty;
    public bool IsBreathing => CurrentInstruction?.BreathingPhase != null;

    public string PhaseSecondsText => IsBreathing
        ? Math.Ceiling(InstructionRemaining.TotalSeconds).ToString("0")
        : string.Empty;

    public double InstructionProgress
    {
        get
        {
            var durationSeconds = CurrentInstruction?.DurationSeconds ?? 0;

            return durationSeconds > 0 ? Math.Clamp(InstructionElapsed.TotalSeconds / durationSeconds, 0, 1) : 0;
        }
    }

    public double BreathingCircleSize
    {
        get
        {
            var easedProgress = (1 - Math.Cos(Math.PI * InstructionProgress)) / 2;
            var sizeDifference = MaxBreathingCircleSize - MinBreathingCircleSize;

            return CurrentInstruction?.BreathingPhase switch
            {
                BreathingPhase.Inhale => MinBreathingCircleSize + sizeDifference * easedProgress,
                BreathingPhase.Exhale => MaxBreathingCircleSize - sizeDifference * easedProgress,
                BreathingPhase.HoldIn => MaxBreathingCircleSize,
                _ => MinBreathingCircleSize
            };
        }
    }

    public WellnessExercisePreviewViewModel(IWellnessExercisePlayer player, TimeProvider timeProvider)
    {
        Dispatcher.UIThread.VerifyAccess();

        _player = player;
        _timeProvider = timeProvider;

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        _timer.Tick += OnTimerTick;
        _player.PlaybackChanged += OnPlaybackChanged;

        SynchronizeTimer();
    }

    /// <summary>
    /// Loads the exercise and displays its first instruction without starting timed playback.
    /// </summary>
    public void Load(WellnessExercise exercise, int cycleCount)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        Dispatcher.UIThread.VerifyAccess();

        // publish only the final paused state, not the temporary state created by Start
        _isLoading = true;

        try
        {
            _player.Start(exercise, cycleCount);

            _exercise = exercise;
            _cycleCount = cycleCount;

            _player.Pause();
        }
        finally
        {
            _isLoading = false;
            RefreshPlayback();
        }
    }

    /// <summary>
    /// Stops playback and removes the loaded exercise from the preview.
    /// </summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        Dispatcher.UIThread.VerifyAccess();

        _exercise = null;
        _cycleCount = 0;

        _timer.Stop();
        _player.Stop();

        // Stop does not raise an event if the player is already stopped or idle
        RefreshPlayback();
    }

    /// <summary>
    /// Resumes paused playback or starts the loaded exercise from the beginning.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPlay))]
    private void Play()
    {
        if (!CanPlay()) return;

        if (IsPaused)
        {
            _player.Resume();
        }
        else
        {
            _player.Start(_exercise!, _cycleCount);
        }
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        if (!CanPause()) return;

        // include time since the last timer tick before pausing
        AdvanceToNow();
        _player.Pause();
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        if (!CanStop()) return;

        _timer.Stop();
        _player.Stop();
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void Next()
    {
        if (!CanNext()) return;

        // time before manual navigation must not be applied to the new instruction
        _lastTimestamp = _timeProvider.GetTimestamp();
        _player.Next();
    }

    [RelayCommand(CanExecute = nameof(CanPrevious))]
    private void Previous()
    {
        if (!CanPrevious()) return;

        _lastTimestamp = _timeProvider.GetTimestamp();
        _player.Previous();
    }

    private bool CanPlay() => !_isDisposed && _exercise != null && !IsPlaying;
    private bool CanPause() => !_isDisposed && IsPlaying;
    private bool CanStop() => !_isDisposed && CurrentInstruction != null;
    private bool CanNext() => !_isDisposed && (IsPlaying || IsPaused);

    private bool CanPrevious() =>
        !_isDisposed && (IsPlaying || IsPaused || State == WellnessExercisePlaybackState.Completed);

    private void OnTimerTick(object? sender, EventArgs e)
    {
        AdvanceToNow();
    }

    private void AdvanceToNow()
    {
        if (_isDisposed || !IsPlaying || !_timer.IsEnabled) return;

        var timestamp = _timeProvider.GetTimestamp();
        var elapsed = _timeProvider.GetElapsedTime(_lastTimestamp, timestamp);

        if (elapsed <= TimeSpan.Zero) return;

        _lastTimestamp = timestamp;
        _player.Advance(elapsed);
    }

    private void OnPlaybackChanged(object? sender, EventArgs e)
    {
        if (_isDisposed || _isLoading) return;

        RefreshPlayback();
    }

    private void RefreshPlayback()
    {
        SynchronizeTimer();
        NotifyPlaybackPropertiesChanged();
        NotifyCommandsCanExecuteChanged();
    }

    private void SynchronizeTimer()
    {
        if (_isDisposed || !IsPlaying)
        {
            _timer.Stop();
            return;
        }

        if (_timer.IsEnabled) return;

        // reset the timestamp so paused time is excluded after resuming
        _lastTimestamp = _timeProvider.GetTimestamp();
        _timer.Start();
    }

    private void NotifyPlaybackPropertiesChanged()
    {
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(CurrentCycle));
        OnPropertyChanged(nameof(TotalCycles));
        OnPropertyChanged(nameof(CurrentInstructionIndex));
        OnPropertyChanged(nameof(CurrentInstruction));
        OnPropertyChanged(nameof(InstructionElapsed));
        OnPropertyChanged(nameof(InstructionRemaining));
        OnPropertyChanged(nameof(InstructionText));
        OnPropertyChanged(nameof(IsBreathing));
        OnPropertyChanged(nameof(PhaseSecondsText));
        OnPropertyChanged(nameof(InstructionProgress));
        OnPropertyChanged(nameof(BreathingCircleSize));
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Stops playback and releases timer and player event subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        Dispatcher.UIThread.VerifyAccess();
        _isDisposed = true;

        _timer.Stop();
        _timer.Tick -= OnTimerTick;
        _player.PlaybackChanged -= OnPlaybackChanged;

        _exercise = null;
        _cycleCount = 0;

        _player.Stop();

        NotifyPlaybackPropertiesChanged();
        NotifyCommandsCanExecuteChanged();
    }
}