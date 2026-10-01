// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a wellness exercise made of ordered instructions.
/// </summary>
internal class WellnessExercise
{
    /// <summary>
    /// Gets or sets the unique identifier for the exercise.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the creation date of the exercise.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the display name of the exercise.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the exercise is used for.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the instructions loop for a chosen number of cycles instead of running once.
    /// </summary>
    public bool IsRepeating { get; set; }

    /// <summary>
    /// Gets or sets the instructions that make up a single cycle of the exercise.
    /// </summary>
    public ICollection<ExerciseInstruction> Instructions { get; set; } = [];
}
