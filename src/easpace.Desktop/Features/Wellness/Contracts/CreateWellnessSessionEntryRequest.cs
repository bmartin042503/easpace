// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents a finished wellness session that can be saved to the history.
/// </summary>
/// <param name="StartDate">The date and time when the session started.</param>
/// <param name="TargetDuration">The planned duration of the session, if it had one.</param>
/// <param name="ActualDuration">The time actually spent in the session.</param>
/// <param name="SessionType">The type of the wellness session.</param>
/// <param name="ExerciseId">The identifier of the exercise used during the session.</param>
/// <param name="ExerciseName">The name of the exercise, saved so the history keeps it after the exercise is deleted.</param>
/// <param name="CompletedCycles">
/// The number of fully completed cycles of a repeating exercise, or <c>null</c> for exercises that run once.
/// Only shown on the ending view; it isn't saved.
/// </param>
internal sealed record CreateWellnessSessionEntryRequest(
    DateTimeOffset StartDate,
    TimeSpan? TargetDuration,
    TimeSpan ActualDuration,
    WellnessSessionType SessionType,
    Guid? ExerciseId,
    string? ExerciseName,
    int? CompletedCycles
);