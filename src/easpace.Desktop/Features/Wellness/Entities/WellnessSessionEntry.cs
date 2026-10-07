// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a recorded wellness session.
/// </summary>
internal class WellnessSessionEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the session.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the session started.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the duration the session lasted.
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Gets or sets the number of cycles used for the exercise.
    /// </summary>
    public int? CycleCount { get; set; }
    
    /// <summary>
    /// Gets or sets the id of the exercise used during the session.
    /// </summary>
    public Guid? ExerciseId { get; set; }
    
    /// <summary>
    /// Gets or sets the exercise used during the session.
    /// </summary>
    public WellnessExercise? Exercise { get; set; }

    /// <summary>
    /// Gets or sets the exercise name used during the session, used as a snapshot in case the exercise is deleted.
    /// </summary>
    public string ExerciseName { get; set; } = string.Empty;
}