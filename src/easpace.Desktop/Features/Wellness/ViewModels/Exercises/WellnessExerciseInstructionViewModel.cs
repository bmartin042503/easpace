// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services.Core;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels.Exercises;

internal partial class WellnessExerciseInstructionViewModel : ViewModelBase
{
    [ObservableProperty] private string _text = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DurationText))]
    private int _durationSeconds;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasBreathingPhase))]
    private BreathingPhase? _breathingPhase;

    public string DurationText =>
        string.Format(LocalizationService.GetString("WellnessExercise.Text.DurationSeconds"), DurationSeconds);

    public bool HasBreathingPhase => BreathingPhase != null;

    public WellnessExerciseInstructionViewModel(WellnessExerciseInstruction instruction)
    {
        Text = instruction.Text;
        DurationSeconds = instruction.DurationSeconds;
        BreathingPhase = instruction.BreathingPhase;
    }

    public WellnessExerciseInstructionViewModel()
    {
        Text = string.Empty;
        DurationSeconds = 0;
        BreathingPhase = null;
    }
}
