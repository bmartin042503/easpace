// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Contracts;

internal sealed record UpsertBreathingTechniqueRequest(
    string Name,
    string Description,
    ICollection<BreathingPhase> Phases,
    int Cycles
);