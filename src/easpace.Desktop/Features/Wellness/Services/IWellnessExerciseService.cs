// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

internal interface IWellnessExerciseService
{
    /// <summary>
    /// Gets the exercises with their ordered instructions, sorted by name.
    /// </summary>
    /// <param name="type">Restricts the result to one session type; <c>null</c> returns all exercises.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<WellnessExercise>> GetExercisesAsync(
        WellnessSessionType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new exercise of the requested type.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the request violates the exercise constraints.</exception>
    Task<WellnessExercise> CreateExerciseAsync(
        UpsertWellnessExerciseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an exercise and replaces all of its instructions. The type of the exercise is kept.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when no exercise exists with the given id.</exception>
    /// <exception cref="ArgumentException">Thrown when the request violates the exercise constraints.</exception>
    Task<WellnessExercise> UpdateExerciseAsync(
        Guid id,
        UpsertWellnessExerciseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an exercise and its instructions. Recorded sessions keep their exercise name.
    /// </summary>
    Task DeleteExerciseAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts the built-in exercises that are missing, matched by session type and name.
    /// </summary>
    /// <returns>The number of restored exercises.</returns>
    Task<int> RestoreDefaultExercisesAsync(CancellationToken cancellationToken = default);
}
