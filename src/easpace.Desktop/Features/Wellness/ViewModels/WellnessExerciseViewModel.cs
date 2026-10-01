// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

/// <summary>
/// Represents a read-only wellness exercise that can be selected for a session.
/// </summary>
internal class WellnessExerciseViewModel : ViewModelBase
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsRepeating { get; }

    /// <summary>
    /// Gets the steps of a single cycle in execution order. Instructions that can't run are left out.
    /// </summary>
    public IReadOnlyList<ExerciseStep> Steps { get; }

    /// <summary>
    /// Gets the duration of a single cycle in seconds.
    /// </summary>
    public int CycleSeconds { get; }

    public WellnessExercise Exercise { get; }

    public WellnessExerciseViewModel(WellnessExercise exercise)
    {
        Id = exercise.Id;
        Name = exercise.Name;
        Description = exercise.Description;
        IsRepeating = exercise.IsRepeating;
        Exercise = exercise;

        // the service validates what it saves, but a session must never start with a step that has
        // no duration or no text, so such instructions are skipped
        Steps = exercise.Instructions
            .OrderBy(i => i.Order)
            .Where(i => i.DurationSeconds > 0 && (!string.IsNullOrWhiteSpace(i.Text) || i.Phase is not null))
            .Select(i => ExerciseStep.Create(i.Text, i.DurationSeconds, i.Phase))
            .ToList();

        CycleSeconds = Steps.Sum(s => s.DurationSeconds);
    }
}
