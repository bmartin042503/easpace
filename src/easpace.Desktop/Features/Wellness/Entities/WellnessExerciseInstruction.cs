// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Entities;

internal class WellnessExerciseInstruction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExerciseId { get; set; }
    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public BreathingPhase? BreathingPhase { get; set; }
}