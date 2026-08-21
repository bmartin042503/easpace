// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

public class BreathingTechniqueViewModel : ViewModelBase
{
    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public string Name { get; }
    public string Description { get; }
    public List<BreathingPhase> Phases { get; }
    public int Cycles { get; }
    public BreathingTechnique BreathingTechnique { get; }
    
    public BreathingTechniqueViewModel(BreathingTechnique breathingTechnique)
    {
        Id = breathingTechnique.Id;
        CreatedAt = breathingTechnique.CreatedAt;
        Name = breathingTechnique.Name;
        Description = breathingTechnique.Description;
        Phases = breathingTechnique.Phases.ToList();
        Cycles = breathingTechnique.Cycles;
        BreathingTechnique = breathingTechnique;
    }
}