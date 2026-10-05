// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace easpace.Desktop.Features.Wellness.Utils;

internal interface IWellnessExerciseClock
{
    event EventHandler<TimeSpan> Tick;
    void Start();
    void Stop();
}