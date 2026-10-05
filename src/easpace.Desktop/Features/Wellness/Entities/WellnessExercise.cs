// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace easpace.Desktop.Features.Wellness.Entities;

/// <summary>
/// Represents a wellness exercise made of ordered instructions.
/// </summary>
internal class WellnessExercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CycleCount { get; set; }
    public ICollection<WellnessExerciseInstruction> Instructions { get; set; } = [];
}