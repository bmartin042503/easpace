// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a single timed instruction within a wellness exercise.
/// </summary>
internal class ExerciseInstruction
{
    /// <summary>
    /// Gets or sets the unique identifier for the instruction.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the identifier of the exercise this instruction belongs to.
    /// </summary>
    public Guid ExerciseId { get; set; }

    /// <summary>
    /// Gets or sets the sequential order of this instruction within the exercise cycle.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the instruction text. May be empty when a breathing phase provides the default text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the instruction in seconds.
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the breathing phase animated during the instruction, if any. Only used by breathing exercises.
    /// </summary>
    public BreathingPhaseType? Phase { get; set; }
}
