// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services.Core;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

/// <summary>
/// Represents the editable fields and instructions of a wellness exercise.
/// </summary>
internal partial class ExerciseFormViewModel : ValidatorViewModelBase
{
    // one cycle can't run longer than an hour
    private const int MaxCycleSeconds = 60 * 60;

    private const int BreathingInstructionSeconds = 4;
    private const int MeditationInstructionSeconds = 60;

    private static readonly IReadOnlyList<WellnessSessionType> AllSessionTypes = Enum.GetValues<WellnessSessionType>();

    // the values the form was loaded with, to tell whether anything changed
    private readonly UpsertWellnessExerciseRequest _original;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "FormValidation.Name.Required")]
    [MaxLength(64, ErrorMessage = "FormValidation.Name.MaxLength")]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(256, ErrorMessage = "FormValidation.Description.MaxLength")]
    private string _description = string.Empty;

    [ObservableProperty] private bool _isRepeating;

    [ObservableProperty] private bool _isDirty;

    /// <summary>
    /// Gets the localization key of the error of the whole instruction list, or <c>null</c> when it's valid.
    /// </summary>
    [ObservableProperty] private string? _instructionsError;

    private WellnessSessionType _type;

    public Guid? Id { get; }
    public bool IsCreatingNew => Id is null;

    public IReadOnlyList<WellnessSessionType> SessionTypes => AllSessionTypes;

    /// <summary>
    /// Gets or sets the session type. It's fixed once the exercise exists, so changes are ignored unless it's new.
    /// </summary>
    public WellnessSessionType Type
    {
        get => _type;
        set
        {
            if (!IsCreatingNew || !SetProperty(ref _type, value)) return;

            OnTypeChanged();
        }
    }

    public AvaloniaList<ExerciseInstructionViewModel> Instructions { get; } = [];

    public int TotalCycleSeconds => Instructions.Sum(i => i.DurationSeconds);

    public string OneCycleDurationText
    {
        get
        {
            var duration = TimeSpan.FromSeconds(TotalCycleSeconds);
            var durationText = duration.ToString(duration.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
            return string.Format(LocalizationService.GetString("Wellness.Editor.Label.OneCycle"), durationText);
        }
    }

    public bool IsValid => !HasErrors && InstructionsError is null && Instructions.All(i => !i.HasErrors);

    /// <summary>
    /// Occurs when a change affects how the exercise runs: its instructions or whether it repeats.
    /// </summary>
    public event EventHandler? DefinitionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExerciseFormViewModel"/> class with the values of an exercise.
    /// </summary>
    public ExerciseFormViewModel(WellnessExercise exercise) : this(exercise.Id, new UpsertWellnessExerciseRequest(
        exercise.Id, exercise.SessionType, exercise.Name, exercise.Description, exercise.IsRepeating,
        exercise.Instructions
            .OrderBy(i => i.Order)
            .Select(i => new UpsertExerciseInstructionRequest(i.Text, i.DurationSeconds,
                exercise.SessionType == WellnessSessionType.Breathing ? i.Phase : null))
            .ToList()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExerciseFormViewModel"/> class for a new, repeating exercise that
    /// starts with one instruction.
    /// </summary>
    /// <param name="type">The initial session type, which can be changed until the exercise is saved.</param>
    public ExerciseFormViewModel(WellnessSessionType type) : this(null, new UpsertWellnessExerciseRequest(
        null, type, string.Empty, string.Empty, true,
        [
            type == WellnessSessionType.Breathing
                ? new UpsertExerciseInstructionRequest(string.Empty, BreathingInstructionSeconds, BreathingPhaseType.Inhale)
                : new UpsertExerciseInstructionRequest(string.Empty, MeditationInstructionSeconds, null)
        ]))
    {
    }

    private ExerciseFormViewModel(Guid? id, UpsertWellnessExerciseRequest original)
    {
        Id = id;
        _type = original.Type;
        _original = original;

        ErrorsChanged += (_, _) => OnPropertyChanged(nameof(IsValid));
        Instructions.CollectionChanged += OnInstructionsCollectionChanged;

        _name = _original.Name;
        _description = _original.Description;
        _isRepeating = _original.IsRepeating;
        Instructions.AddRange(_original.Instructions.Select(i =>
            new ExerciseInstructionViewModel(i.Text, i.DurationSeconds, i.Phase, SupportsPhases)));

        ValidateAllProperties();
    }

    private bool SupportsPhases => Type == WellnessSessionType.Breathing;

    /// <summary>
    /// Validates every field and instruction, so all errors are shown.
    /// </summary>
    public void Validate()
    {
        ValidateAllProperties();

        foreach (var instruction in Instructions)
        {
            instruction.Validate();
        }

        UpdateInstructionsError();
    }

    /// <summary>
    /// Creates the request that saves the form. The values aren't trimmed, the service takes care of that.
    /// </summary>
    public UpsertWellnessExerciseRequest ToRequest() => new(Id, Type, Name, Description, IsRepeating,
        Instructions.Select(i => new UpsertExerciseInstructionRequest(i.Text, i.DurationSeconds, i.Phase)).ToList());

    /// <summary>
    /// Creates the steps of one cycle as the form currently defines them. Instructions that can't run yet are left out.
    /// </summary>
    public IReadOnlyList<ExerciseStep> ToSteps() => Instructions
        .Where(CanRun)
        .Select(i => ExerciseStep.Create(i.Text, i.DurationSeconds, i.Phase))
        .ToList();

    /// <summary>
    /// Marks the instruction that a step of <see cref="ToSteps"/> comes from as the one the preview plays.
    /// </summary>
    /// <param name="stepIndex">The index of the step, or <c>null</c> to mark none.</param>
    public void HighlightStep(int? stepIndex)
    {
        var runnableIndex = 0;

        foreach (var instruction in Instructions)
        {
            var canRun = CanRun(instruction);
            instruction.IsPreviewing = canRun && runnableIndex == stepIndex;

            if (canRun) runnableIndex++;
        }
    }

    private static bool CanRun(ExerciseInstructionViewModel instruction) =>
        instruction.DurationSeconds > 0 && (!string.IsNullOrWhiteSpace(instruction.Text) || instruction.Phase is not null);

    /// <summary>
    /// Appends an instruction: breathing continues with the next phase (inhale, hold, exhale, hold), meditation gets an
    /// empty minute.
    /// </summary>
    [RelayCommand]
    private void AddInstruction()
    {
        Instructions.Add(SupportsPhases
            ? new ExerciseInstructionViewModel(string.Empty, BreathingInstructionSeconds, GetNextPhase(Instructions.LastOrDefault()?.Phase), true)
            : new ExerciseInstructionViewModel(string.Empty, MeditationInstructionSeconds, null, false));
    }

    [RelayCommand]
    private void RemoveInstruction(ExerciseInstructionViewModel? instruction)
    {
        if (instruction is null) return;

        Instructions.Remove(instruction);
    }

    [RelayCommand(CanExecute = nameof(CanMoveInstructionUp))]
    private void MoveInstructionUp(ExerciseInstructionViewModel? instruction)
    {
        if (instruction is null || !CanMoveInstructionUp(instruction)) return;

        var index = Instructions.IndexOf(instruction);
        Instructions.Move(index, index - 1);
    }

    [RelayCommand(CanExecute = nameof(CanMoveInstructionDown))]
    private void MoveInstructionDown(ExerciseInstructionViewModel? instruction)
    {
        if (instruction is null || !CanMoveInstructionDown(instruction)) return;

        var index = Instructions.IndexOf(instruction);
        Instructions.Move(index, index + 1);
    }

    private bool CanMoveInstructionUp(ExerciseInstructionViewModel? instruction) =>
        instruction is not null && Instructions.IndexOf(instruction) > 0;

    private bool CanMoveInstructionDown(ExerciseInstructionViewModel? instruction) =>
        instruction is not null && Instructions.IndexOf(instruction) is var index && index >= 0 && index < Instructions.Count - 1;

    private static BreathingPhaseType GetNextPhase(BreathingPhaseType? previous) => previous switch
    {
        BreathingPhaseType.Inhale => BreathingPhaseType.HoldIn,
        BreathingPhaseType.HoldIn => BreathingPhaseType.Exhale,
        BreathingPhaseType.Exhale => BreathingPhaseType.HoldOut,
        _ => BreathingPhaseType.Inhale
    };

    partial void OnNameChanged(string value) => UpdateIsDirty();

    partial void OnDescriptionChanged(string value) => UpdateIsDirty();

    partial void OnIsRepeatingChanged(bool value) => OnDefinitionChanged();

    partial void OnInstructionsErrorChanged(string? value) => OnPropertyChanged(nameof(IsValid));

    // only breathing offers phases, so the instructions are recreated for the new type, and meditation drops the phases
    private void OnTypeChanged()
    {
        for (var i = 0; i < Instructions.Count; i++)
        {
            var instruction = Instructions[i];
            Instructions[i] = new ExerciseInstructionViewModel(instruction.Text, instruction.DurationSeconds, instruction.Phase, SupportsPhases);
        }

        UpdateIsDirty();
    }

    private void OnInstructionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var instruction in e.OldItems?.OfType<ExerciseInstructionViewModel>() ?? [])
        {
            instruction.PropertyChanged -= OnInstructionPropertyChanged;
            instruction.ErrorsChanged -= OnInstructionErrorsChanged;
        }

        foreach (var instruction in e.NewItems?.OfType<ExerciseInstructionViewModel>() ?? [])
        {
            instruction.PropertyChanged += OnInstructionPropertyChanged;
            instruction.ErrorsChanged += OnInstructionErrorsChanged;
        }

        for (var i = 0; i < Instructions.Count; i++)
        {
            Instructions[i].Number = i + 1;
        }

        MoveInstructionUpCommand.NotifyCanExecuteChanged();
        MoveInstructionDownCommand.NotifyCanExecuteChanged();

        OnDefinitionChanged();
    }

    private void OnInstructionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ExerciseInstructionViewModel.Text)
            or nameof(ExerciseInstructionViewModel.DurationSeconds)
            or nameof(ExerciseInstructionViewModel.SelectedPhase))
        {
            OnDefinitionChanged();
        }
    }

    private void OnInstructionErrorsChanged(object? sender, DataErrorsChangedEventArgs e) => OnPropertyChanged(nameof(IsValid));

    private void OnDefinitionChanged()
    {
        OnPropertyChanged(nameof(TotalCycleSeconds));
        OnPropertyChanged(nameof(OneCycleDurationText));

        UpdateInstructionsError();
        UpdateIsDirty();

        // removing an invalid instruction changes the validity without touching the list error
        OnPropertyChanged(nameof(IsValid));

        DefinitionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateInstructionsError()
    {
        InstructionsError = Instructions.Count == 0 ? "FormValidation.Instructions.Required"
            : TotalCycleSeconds > MaxCycleSeconds ? "FormValidation.Instructions.TooLong"
            : null;
    }

    // compared by value, so reverting a change by hand makes the form clean again
    private void UpdateIsDirty()
    {
        var current = ToRequest();

        IsDirty = current.Type != _original.Type
                  || current.Name != _original.Name
                  || current.Description != _original.Description
                  || current.IsRepeating != _original.IsRepeating
                  || !current.Instructions.SequenceEqual(_original.Instructions);
    }
}
