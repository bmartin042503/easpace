// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessEndingViewModel : ViewModelBase
{
    private WellnessSession _session;

    public event EventHandler? NavigatedToConfiguration;
    
    [ObservableProperty] private bool _askToSaveSession;
    [ObservableProperty] private bool _sessionSaved;
    
    public WellnessEndingViewModel(WellnessSession session)
    {
        _session = session;

        if (_session.ActualDuration == _session.TargetDuration)
        {
            SaveSession();
        }
        else
        {
            AskToSaveSession = true;
        }
    }

    [RelayCommand]
    private void StartAnotherSession()
    {
        NavigatedToConfiguration?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SaveSession()
    {
        AskToSaveSession = false;
        
        // TODO: save session in db   
        SessionSaved = true;
    }

    [RelayCommand]
    private void DiscardSession()
    {
        NavigatedToConfiguration?.Invoke(this, EventArgs.Empty);
    }
}