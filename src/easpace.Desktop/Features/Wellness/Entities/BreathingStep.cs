// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a timed step in a breathing exercise, optionally associated with a breathing phase.
/// </summary>
internal sealed class BreathingStep : ExerciseStep
{
    /// <summary>
    /// Gets or sets the optional breathing phase associated with the step.
    /// </summary>
    public BreathingPhaseType? PhaseType { get; set; }
}