// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a single timed segment within a wellness exercise.
/// </summary>
internal abstract class ExerciseStep
{
    /// <summary>
    /// Gets or sets the unique identifier for the step.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the identifier of the exercise that owns the step.
    /// </summary>
    public Guid WellnessExerciseId { get; set; }
    
    /// <summary>
    /// Gets or sets the position of the step within the exercise sequence.
    /// </summary>
    public int Order { get; set; }
    
    /// <summary>
    /// Gets or sets how long the step lasts, in seconds.
    /// </summary>
    public int DurationSeconds { get; set; }
    
    /// <summary>
    /// Gets or sets the optional instruction displayed during the step.
    /// </summary>
    public string? Instruction { get; set; }
}