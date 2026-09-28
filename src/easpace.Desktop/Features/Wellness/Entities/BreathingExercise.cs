// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a breathing exercise whose instructions may be animated by breathing phases.
/// </summary>
internal class BreathingExercise : WellnessExercise
{
    public override WellnessSessionType SessionType => WellnessSessionType.Breathing;
}
