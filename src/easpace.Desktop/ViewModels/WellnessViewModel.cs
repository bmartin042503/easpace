// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class WellnessViewModel : PageViewModel
{
    #region Fields

    [ObservableProperty] private ObservableObject? _contentViewModel;

    private WellnessConfigurationViewModel? _configurationViewModel;
    private WellnessSessionViewModel? _sessionViewModel;
    private WellnessEndingViewModel? _endingViewModel;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessViewModel"/> class.
    /// </summary>
    public WellnessViewModel()
    {
        Page = ApplicationPage.Wellness;
        SetConfigurationView();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Handles the event when a new session is started from the configuration view.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="sessionConfiguration">The configuration parameters for the new session.</param>
    private void OnSessionStarted(object? sender, WellnessSessionConfiguration sessionConfiguration)
    {
        SetSessionView(sessionConfiguration);
        CleanUpConfigurationView();
    }

    /// <summary>
    /// Handles the event when an active session ends or is manually stopped.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="session">The completed session details.</param>
    private void OnSessionEnded(object? sender, WellnessSession session)
    {
        SetEndingView(session);
        CleanUpSessionView();
    }

    /// <summary>
    /// Handles the event when the user navigates back to the configuration view from the ending view.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void OnNavigatedToConfiguration(object? sender, EventArgs e)
    {
        SetConfigurationView();
        CleanUpEndingView();
    }

    /// <summary>
    /// Sets the current content view to the configuration view and subscribes to its events.
    /// </summary>
    private void SetConfigurationView()
    {
        // initialize configuration view model if it doesn't exist
        if (_configurationViewModel == null)
        {
            _configurationViewModel = new WellnessConfigurationViewModel();
            _configurationViewModel.SessionStarted += OnSessionStarted;
        }

        ContentViewModel = _configurationViewModel;
    }

    /// <summary>
    /// Sets the current content view to the session view and subscribes to its events.
    /// </summary>
    /// <param name="sessionConfiguration">The configuration details to pass to the session view model.</param>
    private void SetSessionView(WellnessSessionConfiguration sessionConfiguration)
    {
        _sessionViewModel = new WellnessSessionViewModel(sessionConfiguration);
        _sessionViewModel.SessionEnded += OnSessionEnded;

        ContentViewModel = _sessionViewModel;
    }

    /// <summary>
    /// Sets the current content view to the ending summary view and subscribes to its events.
    /// </summary>
    /// <param name="session">The session data to display in the ending view model.</param>
    private void SetEndingView(WellnessSession session)
    {
        _endingViewModel = new WellnessEndingViewModel(session);
        _endingViewModel.NavigatedToConfiguration += OnNavigatedToConfiguration;

        ContentViewModel = _endingViewModel;
    }

    /// <summary>
    /// Unsubscribes from events and releases the configuration view model to free up memory.
    /// </summary>
    private void CleanUpConfigurationView()
    {
        if (_configurationViewModel == null) return;

        _configurationViewModel.SessionStarted -= OnSessionStarted;
        _configurationViewModel = null;
    }

    /// <summary>
    /// Unsubscribes from events and releases the session view model to free up memory.
    /// </summary>
    private void CleanUpSessionView()
    {
        if (_sessionViewModel == null) return;

        _sessionViewModel.SessionEnded -= OnSessionEnded;
        _sessionViewModel = null;
    }

    /// <summary>
    /// Unsubscribes from events and releases the ending summary view model to free up memory.
    /// </summary>
    private void CleanUpEndingView()
    {
        if (_endingViewModel == null) return;

        _endingViewModel.NavigatedToConfiguration -= OnNavigatedToConfiguration;
        _endingViewModel = null;
    }

    #endregion
}