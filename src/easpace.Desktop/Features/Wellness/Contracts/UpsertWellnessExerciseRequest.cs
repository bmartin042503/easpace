// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents the data required to create or update a wellness exercise.
/// </summary>
/// <param name="Id">The identifier of an existing exercise, or <c>null</c> for a new one.</param>
/// <param name="Type">The session type of the exercise. Only applied when creating; the type is fixed afterwards.</param>
/// <param name="Name">The display name of the exercise.</param>
/// <param name="Description">The description of the exercise.</param>
/// <param name="IsRepeating">Whether the instructions loop for a chosen number of cycles instead of running once.</param>
/// <param name="Instructions">The instructions in execution order.</param>
internal sealed record UpsertWellnessExerciseRequest(
    Guid? Id,
    WellnessSessionType Type,
    string Name,
    string Description,
    bool IsRepeating,
    IReadOnlyList<UpsertExerciseInstructionRequest> Instructions
);

/// <summary>
/// Represents a single instruction of an exercise create or update request.
/// </summary>
/// <param name="Text">The instruction text; may be empty when a breathing phase is set.</param>
/// <param name="DurationSeconds">The duration of the instruction in seconds.</param>
/// <param name="Phase">The breathing phase of the instruction. Ignored for meditation exercises.</param>
internal sealed record UpsertExerciseInstructionRequest(
    string Text,
    int DurationSeconds,
    BreathingPhaseType? Phase
);
