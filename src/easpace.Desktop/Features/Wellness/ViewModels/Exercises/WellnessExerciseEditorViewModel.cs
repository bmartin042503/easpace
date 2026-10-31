// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Behaviors;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Repositories;
using easpace.Desktop.Features.Wellness.Validation;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels.Exercises;

internal partial class WellnessExerciseEditorViewModel : ViewModelBase
{
    private const int DefaultInstructionDurationSeconds = 5;

    private readonly IWellnessExerciseRepository _wellnessExerciseRepository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessExerciseEditorViewModel> _logger;
    private readonly Guid _exerciseId;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private int _defaultCycleCount;

    public AvaloniaList<WellnessExerciseInstructionViewModel> Instructions { get; } = [];

    public bool HasInstructions => Instructions.Count > 0;

    public event EventHandler? Changed;
    public event EventHandler<WellnessExercise>? Saved;
    public event EventHandler? Canceled;

    public WellnessExerciseEditorViewModel(
        WellnessExercise wellnessExercise,
        IWellnessExerciseRepository wellnessExerciseRepository,
        IDialogService dialogService,
        ILogger<WellnessExerciseEditorViewModel> logger)
    {
        _wellnessExerciseRepository = wellnessExerciseRepository;
        _dialogService = dialogService;
        _logger = logger;
        _exerciseId = wellnessExercise.Id;

        Name = wellnessExercise.Name;
        Description = wellnessExercise.Description;
        DefaultCycleCount = wellnessExercise.DefaultCycleCount;

        // subscribed before adding, so every instruction is tracked the same way
        Instructions.CollectionChanged += OnInstructionsChanged;

        Instructions.AddRange(wellnessExercise.Instructions
            .OrderBy(i => i.Order)
            .Select(i => new WellnessExerciseInstructionViewModel(i)));

        Validate();
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

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (!CanSave()) return;

        try
        {
            _logger.LogInformation("Updating wellness exercise {Id} from editor", _exerciseId);

            var savedExercise =
                await _wellnessExerciseRepository.UpdateWellnessExerciseAsync(_exerciseId, CreateRequest());

            // the exercise no longer exists, so the editor must not close as if it had been saved
            if (savedExercise is null) return;

            Saved?.Invoke(this, savedExercise);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save wellness exercise {Id}", _exerciseId);

            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Wellness.Error.SaveExerciseFailed")
            };

            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    private bool CanSave() => !HasErrors && Instructions.All(i => !i.HasErrors);

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

        OnPropertyChanged(nameof(HasInstructions));
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
        Validate();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Validate()
    {
        SetValidationErrors(WellnessExerciseValidator.Validate(Name, DefaultCycleCount, Instructions.Count));

        // the instructions validate themselves before reporting a change, so their errors are up to date here
        SaveCommand.NotifyCanExecuteChanged();
    }
}
