// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents the configuration parameters required to start a wellness session.
/// </summary>
/// <param name="SessionType">The type of the wellness session.</param>
/// <param name="IsTimerSet">Indicates whether a specific duration timer is set.</param>
/// <param name="TargetDuration">The target duration for the session.</param>
/// <param name="BreathingTechniqueConfiguration">The configuration details for the breathing technique, if applicable.</param>
public record WellnessSessionConfiguration(
    WellnessSessionType SessionType,
    bool IsTimerSet,
    TimeSpan? TargetDuration,
    BreathingTechniqueConfiguration? BreathingTechniqueConfiguration
);