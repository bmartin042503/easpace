// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessViewModel : PageViewModel
{
    [ObservableProperty] private ObservableObject? _contentViewModel;

    private WellnessConfigurationViewModel? _configurationViewModel;
    private WellnessSessionViewModel? _sessionViewModel;
    private WellnessEndingViewModel? _endingViewModel;

    public WellnessViewModel()
    {
        Page = ApplicationPage.Wellness;
        SetConfigurationView();
    }

    private void OnSessionStarted(object? sender, WellnessSessionConfiguration sessionConfiguration)
    {
        SetSessionView(sessionConfiguration);
        CleanUpConfigurationView();
    }

    private void OnSessionEnded(object? sender, WellnessSession session)
    {
        SetEndingView(session);
        CleanUpSessionView();
    }

    private void OnNavigatedToConfiguration(object? sender, EventArgs e)
    {
        SetConfigurationView();
        CleanUpEndingView();
    }

    private void SetConfigurationView()
    {
        if (_configurationViewModel == null)
        {
            _configurationViewModel = new WellnessConfigurationViewModel();
            _configurationViewModel.SessionStarted += OnSessionStarted;
        }

        ContentViewModel = _configurationViewModel;
    }

    private void SetSessionView(WellnessSessionConfiguration sessionConfiguration)
    {
        _sessionViewModel = new WellnessSessionViewModel(sessionConfiguration);
        _sessionViewModel.SessionEnded += OnSessionEnded;
        ContentViewModel = _sessionViewModel;
    }

    private void SetEndingView(WellnessSession session)
    {
        _endingViewModel = new WellnessEndingViewModel(session);
        _endingViewModel.NavigatedToConfiguration += OnNavigatedToConfiguration;
        ContentViewModel = _endingViewModel;
    }

    private void CleanUpConfigurationView()
    {
        if (_configurationViewModel == null) return;
        
        _configurationViewModel.SessionStarted -= OnSessionStarted;
        _configurationViewModel = null;
    }

    private void CleanUpSessionView()
    {
        if (_sessionViewModel == null) return;
        
        _sessionViewModel.SessionEnded -= OnSessionEnded;
        _sessionViewModel = null;
    }

    private void CleanUpEndingView()
    {
        if (_endingViewModel == null) return;
        
        _endingViewModel?.NavigatedToConfiguration -= OnNavigatedToConfiguration;
        _endingViewModel = null;
    }
}