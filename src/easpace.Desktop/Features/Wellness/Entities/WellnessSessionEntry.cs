// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a recorded wellness exercise.
/// </summary>
internal class WellnessSessionEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the recorded session.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the session started.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the originally planned duration of the session.
    /// </summary>
    public TimeSpan? TargetDuration { get; set; }

    /// <summary>
    /// Gets or sets the recorded duration of the session.
    /// </summary>
    public TimeSpan ActualDuration { get; set; }

    /// <summary>
    /// Gets or sets the type of exercise performed during the session.
    /// </summary>
    public WellnessSessionType Type { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the exercise used for the session.
    /// </summary>
    public Guid? WellnessExerciseId { get; set; }
    
    /// <summary>
    /// Gets or sets the exercise associated with the session.
    /// </summary>
    public WellnessExercise? WellnessExercise { get; set; }
    
    /// <summary>
    /// Gets or sets the exercise name captured when the session started.
    /// </summary>
    public string? ExerciseNameSnapshot { get; set; }
    
    /// <summary>
    /// Gets or sets the duration, in seconds, of one complete step sequence as configured when the session started.
    /// </summary>
    public int? SequenceDurationSecondsSnapshot { get; set; }
    
    /// <summary>
    /// Gets or sets the planned total number of complete sequence executions.
    /// </summary>
    public int? TargetCycles { get; set; }

    /// <summary>
    /// Gets or sets the number of fully completed sequence executions.
    /// </summary>
    public int? CompletedCycles { get; set; }
}