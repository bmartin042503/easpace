// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

public interface IBreathingTechniqueService
{
    BreathingTechnique CreateBreathingTechnique(UpsertBreathingTechniqueRequest upsertRequest);
    IReadOnlyList<BreathingTechnique> GetBreathingTechniques();
    BreathingTechnique? UpdateActivity(Guid techniqueId, UpsertBreathingTechniqueRequest upsertRequest);
    bool DeleteBreathingTechnique(Guid techniqueId);
    
}