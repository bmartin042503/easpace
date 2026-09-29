// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Contracts;

namespace easpace.Desktop.Features.Wellness.Services;

internal interface IWellnessSessionManager
{
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// Gets the current state of the session. It is valid right after construction.
    /// </summary>
    SessionProgress Progress { get; }

    /// <summary>
    /// Gets the number of fully completed cycles.
    /// </summary>
    int CompletedCycles { get; }

    /// <summary>
    /// Gets the current diameter of the breathing circle.
    /// </summary>
    double BreathingCircleSize { get; }

    event EventHandler? TimerFinished;

    /// <summary>
    /// Occurs when the session starts, on every tick of the session timer and after every step jump or restart.
    /// </summary>
    event EventHandler<SessionProgress>? ProgressChanged;

    event EventHandler<double>? BreathingCircleAnimationTimerTick;
    
    void StartSession();
    void PauseSession();
    void ResumeSession();
    void StopSession();

    /// <summary>
    /// Jumps to the start of the next step. Jumping past the final step of a finite session finishes it.
    /// A paused session stays paused.
    /// </summary>
    /// <returns><c>false</c> when the session is already completed; otherwise <c>true</c>.</returns>
    bool StepForward();

    /// <summary>
    /// Jumps to the start of the previous step, wrapping back into the previous cycle. A paused session stays paused.
    /// </summary>
    /// <returns><c>false</c> when already at the very first step; otherwise <c>true</c>.</returns>
    bool StepBackward();

    /// <summary>
    /// Returns to the start of the first step and resets the elapsed time. A paused session stays paused.
    /// </summary>
    void Restart();

    string GetTimerText(TimeSpan time);
}