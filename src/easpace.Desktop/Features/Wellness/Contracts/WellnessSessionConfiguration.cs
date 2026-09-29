// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents the configuration parameters required to start a wellness session.
/// </summary>
/// <param name="SessionType">The type of the wellness session.</param>
/// <param name="ExerciseId">The identifier of the exercise the session runs.</param>
/// <param name="ExerciseName">The name of the exercise, captured for the session history.</param>
/// <param name="Steps">The steps of a single cycle, in execution order.</param>
/// <param name="Cycles">The number of cycles to run, or <c>null</c> to loop until the session is stopped.</param>
/// <param name="IsRepeating">Whether the exercise repeats for a chosen number of cycles instead of running once.</param>
internal record WellnessSessionConfiguration(
    WellnessSessionType SessionType,
    Guid? ExerciseId,
    string ExerciseName,
    IReadOnlyList<ExerciseStep> Steps,
    int? Cycles,
    bool IsRepeating
)
{
    /// <summary>
    /// Gets the planned duration of the session, or <c>null</c> when it loops until stopped.
    /// </summary>
    public TimeSpan? TargetDuration =>
        Cycles.HasValue ? TimeSpan.FromSeconds(Cycles.Value * Steps.Sum(s => s.DurationSeconds)) : null;
}