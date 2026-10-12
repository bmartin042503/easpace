// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Behaviors;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels.Exercises;

internal partial class WellnessExerciseEditorViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int _defaultCycleCount;

    public AvaloniaList<WellnessExerciseInstructionViewModel> Instructions { get; } = [];

    public event EventHandler? Canceled;

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
    private void Save()
    {
        // TODO: validate the exercise and save it
    }

    [RelayCommand]
    private void Cancel()
    {
        Canceled?.Invoke(this, EventArgs.Empty);
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
    
    [RelayCommand]
    private void MoveInstruction(ItemMoveRequest request)
    {
        if (request.FromIndex == request.ToIndex
            || request.FromIndex < 0 || request.FromIndex >= Instructions.Count
            || request.ToIndex < 0 || request.ToIndex >= Instructions.Count) return;

        Instructions.Move(request.FromIndex, request.ToIndex);
    }
}
