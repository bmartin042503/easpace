// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a reusable wellness exercise consisting of ordered, timed steps.
/// </summary>
internal abstract class WellnessExercise
{
    /// <summary>
    /// Gets or sets the unique identifier for the exercise.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Gets or sets the date and time when the exercise was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }  = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Gets or sets the display name of the exercise.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description shown when the exercise is selected.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether the complete step sequence can be repeated.
    /// </summary>
    public bool IsRepeatable { get; set; }
    
    /// <summary>
    /// Gets or sets the steps that make up one complete execution of the exercise.
    /// </summary>
    public ICollection<ExerciseStep> Steps { get; set; } = [];
}