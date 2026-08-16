// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace easpace.Desktop.Models;

/// <summary>
/// Represents a specific breathing technique consisting of multiple phases.
/// </summary>
public class BreathingTechnique
{
    /// <summary>
    /// Gets or sets the unique identifier for the breathing technique.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name of the breathing technique.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the breathing technique is used for.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequence of phases that make up a single cycle of this technique.
    /// </summary>
    public List<BreathingPhase> Phases { get; set; } = [];

    /// <summary>
    /// Gets or sets the default or target number of cycles for this technique.
    /// </summary>
    public int Cycles { get; set; }
}