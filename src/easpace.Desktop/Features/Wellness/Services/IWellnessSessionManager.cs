// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Contracts;

namespace easpace.Desktop.Features.Wellness.Services;

public interface IWellnessSessionManager
{
    TimeSpan ElapsedTime { get; }
    
    event EventHandler? TimerFinished;
    event EventHandler<SessionTexts>? TimerTick;
    event EventHandler<double>? BreathingCircleAnimationTimerTick;
    
    void StartSession();
    void PauseSession();
    void ResumeSession();
    void StopSession();
    string GetTimerText(TimeSpan time);
}