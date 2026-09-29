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

    event EventHandler? TimerFinished;

    /// <summary>
    /// Occurs when the session starts and on every tick of the session timer.
    /// </summary>
    event EventHandler<SessionProgress>? ProgressChanged;

    event EventHandler<double>? BreathingCircleAnimationTimerTick;
    
    void StartSession();
    void PauseSession();
    void ResumeSession();
    void StopSession();
    string GetTimerText(TimeSpan time);
}