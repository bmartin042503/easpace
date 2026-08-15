// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessConfigurationViewModel : ValidatorViewModelBase
{
    [ObservableProperty] private int _durationMinutes = 5;

    [ObservableProperty] private bool _isTimerChecked = true;

    [NotifyPropertyChangedFor(nameof(IsBreathingTechniquesVisible))] 
    [ObservableProperty]
    private WellnessSessionType _selectedSessionType = WellnessSessionType.Breathing;

    [ObservableProperty] private BreathingTechnique? _selectedBreathingTechnique;

    public event EventHandler<WellnessSessionConfiguration>? SessionStarted;

    public ObservableCollection<BreathingTechnique> BreathingTechniques { get; } = [];

    public bool IsBreathingTechniquesVisible => SelectedSessionType == WellnessSessionType.Breathing;

    public WellnessConfigurationViewModel()
    {
        InitializeStockBreathingTechniques();
        SelectedBreathingTechnique = BreathingTechniques.FirstOrDefault();
    }

    [RelayCommand]
    private void StartSession()
    {
        TimeSpan targetDuration;
        BreathingTechniqueConfiguration? breathingTechniqueConfiguration = null;
        
        // set the breathing technique
        if (SelectedSessionType == WellnessSessionType.Breathing && SelectedBreathingTechnique != null)
        {
            // get the seconds of a single cycle
            var cycleDurationSeconds = SelectedBreathingTechnique.Phases.Sum(p => p.DurationSeconds);

            int cycles;

            if (IsTimerChecked)
            {
                // requested seconds by the user
                var requestedSeconds = DurationMinutes * 60;
                
                // count of cycles that can fit in the requested timespan
                cycles = Math.Max(1, requestedSeconds / cycleDurationSeconds);
                
                targetDuration = TimeSpan.FromSeconds(cycles * cycleDurationSeconds);
            }
            else
            {
                cycles = SelectedBreathingTechnique.Cycles;
                targetDuration = TimeSpan.Zero;
            }

            breathingTechniqueConfiguration = new BreathingTechniqueConfiguration(
                BreathingTechnique: SelectedBreathingTechnique,
                Cycles: cycles
            );
        }
        else
        {
            targetDuration = IsTimerChecked ? TimeSpan.FromMinutes(DurationMinutes) : TimeSpan.Zero;
        }

        var sessionConfiguration = new WellnessSessionConfiguration(
            SessionType: SelectedSessionType,
            TargetDuration: targetDuration,
            IsTimerSet: IsTimerChecked,
            BreathingTechniqueConfiguration: breathingTechniqueConfiguration
        );

        SessionStarted?.Invoke(this, sessionConfiguration);
    }

    // TODO: save it in the db and get all instead of this
    private void InitializeStockBreathingTechniques()
    {
        BreathingTechniques.Add(
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