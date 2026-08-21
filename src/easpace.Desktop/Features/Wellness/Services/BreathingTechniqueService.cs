// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

// Temporary in-memory implementation.
public class BreathingTechniqueService : IBreathingTechniqueService
{
    private readonly List<BreathingTechnique> _breathingTechniques = [];

    public BreathingTechniqueService()
    {
        InitializeStockBreathingTechniques();
    }

    public BreathingTechnique CreateBreathingTechnique(UpsertBreathingTechniqueRequest upsertRequest)
    {
        var breathingTechnique = new BreathingTechnique
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Name = upsertRequest.Name,
            Description = upsertRequest.Description,
            Phases = upsertRequest.Phases,
            Cycles = upsertRequest.Cycles,
        };

        _breathingTechniques.Add(breathingTechnique);

        return breathingTechnique;
    }

    public IReadOnlyList<BreathingTechnique> GetBreathingTechniques() => 
        _breathingTechniques.OrderByDescending(t => t.CreatedAt).ToList();

    public BreathingTechnique? UpdateActivity(Guid techniqueId, UpsertBreathingTechniqueRequest upsertRequest)
    {
        var technique = _breathingTechniques.FirstOrDefault(t => t.Id == techniqueId);

        if (technique == null) return null;

        technique.Name = upsertRequest.Name;
        technique.Description = upsertRequest.Description;
        technique.Phases = upsertRequest.Phases;
        technique.Cycles = upsertRequest.Cycles;

        return technique;
    }

    public bool DeleteBreathingTechnique(Guid techniqueId)
    {
        var technique = _breathingTechniques.FirstOrDefault(t => t.Id == techniqueId);
        return technique is not null && _breathingTechniques.Remove(technique);
    }
    
    private void InitializeStockBreathingTechniques()
    {
        _breathingTechniques.Add(
            new BreathingTechnique
            {
                Name = "Box Breathing",
                Description = "Box Breathing Description",
                Phases =
                [
                    new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 4 },
                    new BreathingPhase { Type = BreathingPhaseType.HoldIn, DurationSeconds = 4 },
                    new BreathingPhase { Type = BreathingPhaseType.Exhale, DurationSeconds = 4 },
                    new BreathingPhase { Type = BreathingPhaseType.HoldOut, DurationSeconds = 4 }
                ],
                Cycles = 4
            }
        );
    }
}