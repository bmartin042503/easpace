// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models;

public record WellnessSessionConfiguration(
    WellnessSessionType SessionType,
    bool IsTimerSet,
    TimeSpan TargetDuration,
    BreathingTechniqueConfiguration? BreathingTechniqueConfiguration
);