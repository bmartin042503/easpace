// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessViewModel : PageViewModel
{
    [ObservableProperty] private ViewModelBase _content;
    
    private WellnessStartViewModel? _startViewModel;
    private WellnessSettingsViewModel? _settingsViewModel;
    private WellnessSessionViewModel? _sessionViewModel;
    
    public WellnessViewModel()
    {
        Page = ApplicationPage.Wellness;
        
        _startViewModel = new WellnessStartViewModel();
        _startViewModel.SessionSelected += OnSessionSelected;
        
        Content = _startViewModel;
    }

    private void OnSessionSelected(object? sender, WellnessSessionType sessionType)
    {
        _settingsViewModel = new WellnessSettingsViewModel
        {
            SelectedSessionType = sessionType
        };
        
        _settingsViewModel.NavigatedBack += OnNavigatedBack;
        _settingsViewModel.SessionStarted += OnSessionStarted;
        
        Content = _settingsViewModel;
    }

    private void OnNavigatedBack(object? sender, EventArgs e)
    {
        if (_startViewModel == null) return;
        Content = _startViewModel;
    }

    private void OnSessionStarted(object? sender, WellnessSession session)
    {
        _sessionViewModel = new WellnessSessionViewModel(session);
        Content = _sessionViewModel;
        
        _startViewModel?.SessionSelected -= OnSessionSelected;
        _settingsViewModel?.NavigatedBack -= OnNavigatedBack;
        _settingsViewModel?.SessionStarted -= OnSessionStarted;
        
        _startViewModel = null;
        _settingsViewModel = null;
    }
}