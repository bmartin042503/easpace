// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;

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
    /// Gets or sets the originally planned duration for the session.
    /// </summary>
    public TimeSpan? TargetDuration { get; set; }

    /// <summary>
    /// Gets or sets the actual duration the session lasted.
    /// </summary>
    public TimeSpan ActualDuration { get; set; }

    /// <summary>
    /// Gets or sets the type of the wellness session (e.g., breathing, meditation).
    /// </summary>
    public WellnessSessionType Type { get; set; }

    /// <summary>
    /// Gets or sets the exercise id used during the session, if the exercise still exists.
    /// </summary>
    public Guid? ExerciseId { get; set; }

    /// <summary>
    /// Gets or sets the exercise used during the session, if the exercise still exists.
    /// </summary>
    public WellnessExercise? Exercise { get; set; }

    /// <summary>
    /// Gets or sets the exercise name captured when the session was saved, so history survives the exercise's deletion.
    /// </summary>
    public string? ExerciseName { get; set; }
}