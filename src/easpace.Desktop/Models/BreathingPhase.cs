// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models;

public class BreathingPhase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public BreathingPhaseType Type { get; set; }
    public int DurationSeconds { get; set; }
}