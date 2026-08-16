// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models;

/// <summary>
/// Represents a single phase within a breathing technique cycle.
/// </summary>
public class BreathingPhase
{
    /// <summary>
    /// Gets or sets the unique identifier for the breathing phase.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the type of the breathing action (e.g., inhale, exhale).
    /// </summary>
    public BreathingPhaseType Type { get; set; }

    /// <summary>
    /// Gets or sets the duration of the phase in seconds.
    /// </summary>
    public int DurationSeconds { get; set; }
}