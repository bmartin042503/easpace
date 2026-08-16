// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;
using easpace.Desktop.Services;

namespace easpace.Desktop.ViewModels;

public partial class WellnessEndingViewModel : ViewModelBase
{
    private WellnessSession _session;

    public event EventHandler? NavigatedToConfiguration;

    [ObservableProperty] private bool _askToSaveSession;
    [ObservableProperty] private bool _sessionSaved;
    [ObservableProperty] private string _titleText;

    public string DurationText { get; set; }
    public WellnessSessionType SessionType { get; set; }
    public bool IsBreathingType { get; set; }
    public string BreathingTechniqueName { get; set; }
    public int CycleCount { get; set; }

    public WellnessEndingViewModel(WellnessSession session)
    {
        _session = session;

        if (_session.ActualDuration == _session.TargetDuration)
        {
            SaveSession();
        }
        else
        {
            TitleText = LocalizationService.GetString("Wellness.Question.SaveSession");
            AskToSaveSession = true;
        }

        DurationText =
            _session.ActualDuration.ToString(_session.ActualDuration.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
        
        SessionType = _session.Type;
        
        IsBreathingType = SessionType == WellnessSessionType.Breathing;

        if (IsBreathingType && _session.BreathingTechnique != null)
        {
            BreathingTechniqueName = _session.BreathingTechnique.Name;
            CycleCount = _session.ActualBreathingCycles;
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        NavigatedToConfiguration?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SaveSession()
    {
        AskToSaveSession = false;

        // TODO: save session in db   
        SessionSaved = true;

        TitleText = LocalizationService.GetString("Wellness.Text.SessionSaved");
    }
}