// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessSettingsViewModel : ViewModelBase
{
    [ObservableProperty] private int _durationMinutes;
    [ObservableProperty] private bool _isBackgroundAudioChecked;
    
    [NotifyPropertyChangedFor(nameof(IsBreathingTechniquesVisible))]
    [ObservableProperty] 
    private WellnessSessionType _selectedSessionType;

    public event EventHandler<WellnessSession>? SessionStarted;
    public event EventHandler? NavigatedBack;
    
    public ObservableCollection<BreathingTechnique> BreathingTechniques { get; } = [];

    public bool IsBreathingTechniquesVisible => SelectedSessionType == WellnessSessionType.Breathing;

    public WellnessSettingsViewModel()
    {
        InitializeStockBreathingTechniques();
    }

    private void InitializeStockBreathingTechniques()
    {
        BreathingTechniques.Add(
            new BreathingTechnique
            {
                Name = "Box Breathing",
                Description = "Box Breathing Description",
                Phases = [
                    new BreathingPhase
                    {
                        Type = BreathingPhaseType.Inhale,
                        DurationSeconds = 4
                    },
                    new BreathingPhase
                    {
                        Type = BreathingPhaseType.HoldIn,
                        DurationSeconds = 4
                    },
                    new BreathingPhase
                    {
                        Type = BreathingPhaseType.Exhale,
                        DurationSeconds = 4
                    },
                    new BreathingPhase
                    {
                        Type = BreathingPhaseType.HoldOut,
                        DurationSeconds = 4
                    },
                ],
                Cycles = 4
            }
        );
    }

    [RelayCommand]
    private void StartSession()
    {
        var session = new WellnessSession
        {
            StartDate = DateTimeOffset.Now,
            Type = SelectedSessionType
        };
        
        SessionStarted?.Invoke(this, session);
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigatedBack?.Invoke(this, EventArgs.Empty);
    }
}