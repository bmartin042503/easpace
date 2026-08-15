// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessSessionViewModel : ViewModelBase
{
    private DispatcherTimer? _timer;
    private TimeSpan _timeLeft;
    private TimeSpan _elapsedTime;
    private DateTimeOffset _startDate;
    
    private WellnessSessionConfiguration _sessionConfiguration;

    [ObservableProperty] private string _timerText = "00:00";

    public event EventHandler<WellnessSession>? SessionEnded;
    
    public WellnessSessionViewModel(WellnessSessionConfiguration sessionConfiguration)
    {
        _sessionConfiguration = sessionConfiguration;
        _elapsedTime = TimeSpan.Zero;
        
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;

        if (_sessionConfiguration.IsTimerSet)
        {
            _timeLeft = _sessionConfiguration.TargetDuration;
            UpdateTimerText(_timeLeft);
        }
        else
        {
            UpdateTimerText(_elapsedTime);
        }
        
        _timer.Start();
        _startDate = DateTimeOffset.Now;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _elapsedTime = _elapsedTime.Add(TimeSpan.FromSeconds(1));

        if (_sessionConfiguration.IsTimerSet)
        {
            _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
            UpdateTimerText(_timeLeft);

            if (_timeLeft.TotalSeconds <= 0)
            {
                FinishSession();
            }
        }
        else
        {
            UpdateTimerText(_elapsedTime);
        }
    }

    [RelayCommand]
    private void StopSession()
    {
        FinishSession();
    }

    private void FinishSession()
    {
        _timer?.Stop();
        _timer?.Tick -= OnTimerTick;

        BreathingTechnique? techniqueToSave = null;
        
        if (_sessionConfiguration.BreathingTechniqueConfiguration != null)
        {
            var configTech = _sessionConfiguration.BreathingTechniqueConfiguration.BreathingTechnique;
            if (configTech != null)
            {
                techniqueToSave = new BreathingTechnique
                {
                    Id = configTech.Id,
                    Name = configTech.Name,
                    Description = configTech.Description,
                    Phases = configTech.Phases,
                    Cycles = _sessionConfiguration.BreathingTechniqueConfiguration.Cycles
                };
            }
        }

        var session = new WellnessSession
        {
            Id = Guid.NewGuid(),
            StartDate = _startDate,
            TargetDuration = _sessionConfiguration.TargetDuration,
            ActualDuration = _elapsedTime,
            Type = _sessionConfiguration.SessionType,
            BreathingTechnique = techniqueToSave
        };
        
        SessionEnded?.Invoke(this, session);
    }

    private void UpdateTimerText(TimeSpan time)
    {
        TimerText = time.ToString(time.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
    }
}