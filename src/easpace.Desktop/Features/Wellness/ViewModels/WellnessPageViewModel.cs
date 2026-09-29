// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Data;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal partial class WellnessPageViewModel : PageViewModel
{
    #region Fields

    private readonly IMessenger _messenger;
    private readonly IWindowService _windowService;
    private readonly IPreferencesService _preferencesService;
    private readonly IWellnessSessionEntryService _wellnessSessionEntryService;
    private readonly IWellnessExerciseService _wellnessExerciseService;
    private readonly IDialogService _dialogService;
    private readonly IToastMessageService _toastMessageService;
    private readonly ILogger<WellnessStartViewModel> _startLogger;
    private readonly ILogger<WellnessEndingViewModel> _endingLogger;
    private readonly ILogger<WellnessExerciseEditorViewModel> _exerciseEditorLogger;

    [ObservableProperty] private ObservableObject? _contentViewModel;
    [ObservableProperty] private bool _isBlobBackgroundVisible;

    private WellnessStartViewModel? _configurationViewModel;
    private WellnessSessionViewModel? _sessionViewModel;
    private WellnessEndingViewModel? _endingViewModel;
    private WellnessExerciseEditorViewModel? _exerciseEditorViewModel;

    private bool _isFullScreenSettingOn;
    private bool _isAnimatedBgSettingOn;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessPageViewModel"/> class.
    /// </summary>
    public WellnessPageViewModel(
        IMessenger messenger,
        IWindowService windowService,
        IPreferencesService preferencesService,
        IWellnessSessionEntryService wellnessSessionEntryService,
        IWellnessExerciseService wellnessExerciseService,
        IDialogService dialogService,
        IToastMessageService toastMessageService,
        ILogger<WellnessStartViewModel> startLogger,
        ILogger<WellnessEndingViewModel> endingLogger,
        ILogger<WellnessExerciseEditorViewModel> exerciseEditorLogger)
    {
        Page = ApplicationPage.Wellness;
        _messenger = messenger;
        _windowService = windowService;
        _preferencesService = preferencesService;
        _wellnessSessionEntryService = wellnessSessionEntryService;
        _wellnessExerciseService = wellnessExerciseService;
        _dialogService = dialogService;
        _toastMessageService = toastMessageService;

        _startLogger = startLogger;
        _endingLogger = endingLogger;
        _exerciseEditorLogger = exerciseEditorLogger;

        LoadWellnessSettings();
        
        SetConfigurationView();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Reloads the wellness settings each time the page is shown, so changes made in Settings take effect.
    /// </summary>
    [RelayCommand]
    public void Initialize()
    {
        LoadWellnessSettings();
    }

    #endregion

    #region Private Helper Methods

    private void LoadWellnessSettings()
    {
        _isAnimatedBgSettingOn = _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessAnimatedBackground);
        _isFullScreenSettingOn = _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessFullScreen);
    }

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
    /// <param name="createEntryRequest">The completed session's create request.</param>
    private void OnSessionEnded(object? sender, CreateWellnessSessionEntryRequest createEntryRequest)
    {
        SetEndingView(createEntryRequest);
        CleanUpSessionView();
    }

    /// <summary>
    /// Handles the event when the user navigates back to the configuration view from the ending view.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void OnNavigatedToConfiguration(object? sender, EventArgs e)
    {
        if (_isFullScreenSettingOn)
        {
            _windowService.ExitFullScreen();
        }
        
        _messenger.Send(new ApplicationMessage.SidebarVisibility(true));

        SetConfigurationView();
        CleanUpEndingView();
    }

    /// <summary>
    /// Handles the event when the user wants to manage the exercises from the configuration view.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="selectedExerciseId">The exercise selected in the configuration view, if any.</param>
    private void OnManageExercisesRequested(object? sender, Guid? selectedExerciseId)
    {
        SetExerciseEditorView(selectedExerciseId);
    }

    /// <summary>
    /// Handles the event when the user leaves the exercise editor. The same configuration view is shown again,
    /// and its exercises are reloaded, as they may have changed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void OnExerciseEditorClosed(object? sender, EventArgs e)
    {
        SetConfigurationView();
        CleanUpExerciseEditorView();

        // the refresh reports its own errors, so nothing escapes this async event handler
        if (_configurationViewModel is { } configurationViewModel)
        {
            await configurationViewModel.RefreshExercisesAsync();
        }
    }

    /// <summary>
    /// Sets the current content view to the configuration view and subscribes to its events.
    /// </summary>
    private void SetConfigurationView()
    {
        // always false
        IsBlobBackgroundVisible = false;
        
        // initialize configuration view model if it doesn't exist
        if (_configurationViewModel == null)
        {
            _configurationViewModel = new WellnessStartViewModel(
                _wellnessSessionEntryService, _wellnessExerciseService, _dialogService, _startLogger);

            _configurationViewModel.SessionStarted += OnSessionStarted;
            _configurationViewModel.ManageExercisesRequested += OnManageExercisesRequested;
        }

        ContentViewModel = _configurationViewModel;
    }

    /// <summary>
    /// Sets the current content view to the session view and subscribes to its events.
    /// </summary>
    /// <param name="sessionConfiguration">The configuration details to pass to the session view model.</param>
    private void SetSessionView(WellnessSessionConfiguration sessionConfiguration)
    {
        IsBlobBackgroundVisible = _isAnimatedBgSettingOn;

        if (_isFullScreenSettingOn)
        {
            _windowService.EnterFullScreen();
        }
        
        _messenger.Send(new ApplicationMessage.SidebarVisibility(false));

        _sessionViewModel = new WellnessSessionViewModel(_preferencesService, sessionConfiguration);
        _sessionViewModel.SessionEnded += OnSessionEnded;

        ContentViewModel = _sessionViewModel;
    }

    /// <summary>
    /// Sets the current content view to the ending summary view and subscribes to its events.
    /// </summary>
    /// <param name="createEntryRequest">A create request to pass to the ending view model for saving.</param>
    private void SetEndingView(CreateWellnessSessionEntryRequest createEntryRequest)
    {
        IsBlobBackgroundVisible = _isAnimatedBgSettingOn;
        
        _endingViewModel = new WellnessEndingViewModel(_wellnessSessionEntryService, _dialogService, createEntryRequest, _endingLogger);
        _endingViewModel.NavigatedToConfiguration += OnNavigatedToConfiguration;

        ContentViewModel = _endingViewModel;
    }

    /// <summary>
    /// Sets the current content view to the exercise editor and subscribes to its events.
    /// The configuration view model is kept, so its state is intact when the editor is closed.
    /// </summary>
    /// <param name="selectedExerciseId">The exercise to select in the editor, if any.</param>
    private void SetExerciseEditorView(Guid? selectedExerciseId)
    {
        IsBlobBackgroundVisible = false;

        _exerciseEditorViewModel = new WellnessExerciseEditorViewModel(
            _wellnessExerciseService, _dialogService, _toastMessageService, _exerciseEditorLogger, selectedExerciseId);
        _exerciseEditorViewModel.Closed += OnExerciseEditorClosed;

        ContentViewModel = _exerciseEditorViewModel;
    }

    /// <summary>
    /// Unsubscribes from events and releases the configuration view model to free up memory.
    /// </summary>
    private void CleanUpConfigurationView()
    {
        if (_configurationViewModel == null) return;

        _configurationViewModel.SessionStarted -= OnSessionStarted;
        _configurationViewModel.ManageExercisesRequested -= OnManageExercisesRequested;
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

    /// <summary>
    /// Unsubscribes from events, stops the remaining work of the exercise editor and releases it.
    /// </summary>
    private void CleanUpExerciseEditorView()
    {
        if (_exerciseEditorViewModel == null) return;

        _exerciseEditorViewModel.Closed -= OnExerciseEditorClosed;
        _exerciseEditorViewModel.Close();
        _exerciseEditorViewModel = null;
    }

    #endregion
}