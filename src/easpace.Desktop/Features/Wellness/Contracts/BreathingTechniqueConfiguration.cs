// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents the configuration applied to a specific breathing technique for a session.
/// </summary>
/// <param name="BreathingTechnique">The breathing technique selected for the session.</param>
/// <param name="Cycles">The number of cycles to be performed.</param>
public record BreathingTechniqueConfiguration(
    BreathingTechnique? BreathingTechnique,
    int Cycles
);