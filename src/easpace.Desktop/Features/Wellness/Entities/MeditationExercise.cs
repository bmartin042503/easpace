// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a meditation exercise made of timed text instructions.
/// </summary>
internal class MeditationExercise : WellnessExercise
{
    public override WellnessSessionType SessionType => WellnessSessionType.Meditation;
}
