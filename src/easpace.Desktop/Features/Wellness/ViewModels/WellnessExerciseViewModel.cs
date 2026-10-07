// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal class WellnessExerciseViewModel : ViewModelBase
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int DefaultCycleCount { get; }
    public IReadOnlyList<WellnessExerciseInstruction> Instructions { get; }
    
    public WellnessExercise Exercise { get; }
    
    public WellnessExerciseViewModel(WellnessExercise wellnessExercise)
    {
        Id = wellnessExercise.Id;
        Name = wellnessExercise.Name;
        Description = wellnessExercise.Description;
        DefaultCycleCount = wellnessExercise.DefaultCycleCount;
        Instructions = wellnessExercise.Instructions.ToList();
        Exercise = wellnessExercise;
    }
}