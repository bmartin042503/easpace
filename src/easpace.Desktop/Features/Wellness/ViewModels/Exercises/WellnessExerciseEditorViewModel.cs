// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels.Exercises;

internal partial class WellnessExerciseEditorViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int _defaultCycleCount;

    public AvaloniaList<WellnessExerciseInstructionViewModel> Instructions { get; } = [];

    public WellnessExerciseEditorViewModel(WellnessExercise wellnessExercise)
    {
        Name = wellnessExercise.Name;
        Description = wellnessExercise.Description;
        DefaultCycleCount = wellnessExercise.DefaultCycleCount;
        
        Instructions.AddRange(wellnessExercise.Instructions
            .OrderBy(i => i.Order)
            .Select(i => new WellnessExerciseInstructionViewModel(i)));
    }

    [RelayCommand]
    private void AddInstruction()
    {
        Instructions.Add(new WellnessExerciseInstructionViewModel());
    }

    [RelayCommand]
    private void RemoveInstruction(WellnessExerciseInstructionViewModel? instructionVm)
    {
        if (instructionVm is null) return;
        Instructions.Remove(instructionVm);
    }
}