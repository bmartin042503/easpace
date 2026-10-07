// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Contracts;

internal sealed record UpsertWellnessExerciseInstructionRequest(
    string Text,
    int DurationSeconds,
    BreathingPhase? BreathingPhase
);

internal sealed record UpsertWellnessExerciseRequest(
    string Name,
    string Description,
    int DefaultCycleCount,
    IReadOnlyList<UpsertWellnessExerciseInstructionRequest> Instructions
);