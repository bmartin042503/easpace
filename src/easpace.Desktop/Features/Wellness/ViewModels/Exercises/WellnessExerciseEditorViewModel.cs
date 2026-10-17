// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Behaviors;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels.Exercises;

internal partial class WellnessExerciseEditorViewModel : ViewModelBase
{
    private const int DefaultInstructionDurationSeconds = 5;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int _defaultCycleCount;

    public AvaloniaList<WellnessExerciseInstructionViewModel> Instructions { get; } = [];
    
    public event EventHandler? Changed;
    public event EventHandler? Canceled;

    public WellnessExerciseEditorViewModel(WellnessExercise wellnessExercise)
    {
        Name = wellnessExercise.Name;
        Description = wellnessExercise.Description;
        DefaultCycleCount = wellnessExercise.DefaultCycleCount;

        // subscribed before adding, so every instruction is tracked the same way
        Instructions.CollectionChanged += OnInstructionsChanged;

        Instructions.AddRange(wellnessExercise.Instructions
            .OrderBy(i => i.Order)
            .Select(i => new WellnessExerciseInstructionViewModel(i)));
    }
    
    public UpsertWellnessExerciseRequest CreateRequest()
    {
        return new UpsertWellnessExerciseRequest(
            Name,
            Description,
            DefaultCycleCount,
            Instructions
                .Select(i => new UpsertWellnessExerciseInstructionRequest(i.Text, i.DurationSeconds, i.BreathingPhase))
                .ToList());
    }

    partial void OnNameChanged(string value) => OnChanged();
    partial void OnDescriptionChanged(string value) => OnChanged();
    partial void OnDefaultCycleCountChanged(int value) => OnChanged();

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
        Instructions.Add(new WellnessExerciseInstructionViewModel
        {
            DurationSeconds = DefaultInstructionDurationSeconds
        });
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

    private void OnInstructionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // a move reports the item as both old and new, so it stays subscribed once
        if (e.OldItems != null)
        {
            foreach (WellnessExerciseInstructionViewModel instruction in e.OldItems)
            {
                instruction.PropertyChanged -= OnInstructionPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (WellnessExerciseInstructionViewModel instruction in e.NewItems)
            {
                instruction.PropertyChanged += OnInstructionPropertyChanged;
            }
        }

        OnChanged();
    }

    private void OnInstructionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // derived display properties would report the same change again
        if (e.PropertyName is nameof(WellnessExerciseInstructionViewModel.Text)
            or nameof(WellnessExerciseInstructionViewModel.DurationSeconds)
            or nameof(WellnessExerciseInstructionViewModel.BreathingPhase))
        {
            OnChanged();
        }
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
