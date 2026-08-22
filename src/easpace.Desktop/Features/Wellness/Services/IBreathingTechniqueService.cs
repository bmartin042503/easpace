// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

public interface IBreathingTechniqueService
{
    Task<BreathingTechnique> CreateBreathingTechniqueAsync(UpsertBreathingTechniqueRequest upsertRequest);
    Task<IReadOnlyList<BreathingTechnique>> GetBreathingTechniquesAsync();
    Task<BreathingTechnique?> UpdateBreathingTechniqueAsync(Guid techniqueId, UpsertBreathingTechniqueRequest upsertRequest);
    Task<bool> DeleteBreathingTechniqueAsync(Guid techniqueId);
}