// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

internal interface IWellnessExerciseService
{
    /// <summary>
    /// Gets the exercises with their ordered instructions, sorted by name.
    /// </summary>
    Task<IReadOnlyList<WellnessExercise>> GetExercisesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new exercise.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the request violates the exercise constraints.</exception>
    Task<WellnessExercise> CreateExerciseAsync(
        UpsertWellnessExerciseRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an exercise and replaces all of its instructions.
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
}
