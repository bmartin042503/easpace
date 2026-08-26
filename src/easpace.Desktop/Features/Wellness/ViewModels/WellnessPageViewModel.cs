// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal partial class WellnessPageViewModel : PageViewModel
{
    #region Fields

    private readonly IMessenger _messenger;
    private readonly IWindowService _windowService;
    private readonly IWellnessSessionEntryService _wellnessSessionEntryService;
    private readonly IBreathingTechniqueService _breathingTechniqueService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessPageViewModel> _logger;
    private readonly ILogger<WellnessStartViewModel> _startLogger;
    private readonly ILogger<WellnessEndingViewModel> _endingLogger;

    [ObservableProperty] private ObservableObject? _contentViewModel;
    [ObservableProperty] private bool _isBlobBackgroundVisible;

    private WellnessStartViewModel? _configurationViewModel;
    private WellnessSessionViewModel? _sessionViewModel;
    private WellnessEndingViewModel? _endingViewModel;

    private bool _isInitialized;

    public AvaloniaList<WellnessSessionEntryViewModel> SessionEntries { get; } = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="WellnessPageViewModel"/> class.
    /// </summary>
    public WellnessPageViewModel(
        IMessenger messenger,
        IWindowService windowService,
        IWellnessSessionEntryService wellnessSessionEntryService,
        IBreathingTechniqueService breathingTechniqueService,
        IDialogService dialogService,
        ILogger<WellnessPageViewModel> logger,
        ILogger<WellnessStartViewModel> startLogger,
        ILogger<WellnessEndingViewModel> endingLogger)
    {
        Page = ApplicationPage.Wellness;
        _messenger = messenger;
        _windowService = windowService;
        _wellnessSessionEntryService = wellnessSessionEntryService;
        _breathingTechniqueService = breathingTechniqueService;
        _dialogService = dialogService;

        _logger = logger;
        _startLogger = startLogger;
        _endingLogger = endingLogger;

        SetConfigurationView();
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            await LoadSessionEntries();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize wellness page and load entries");
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task LoadSessionEntries()
    {
        SessionEntries.Clear();

        var entries = await _wellnessSessionEntryService.GetWellnessSessionEntriesAsync();

        var entryViewModels = entries.Select(entry => new WellnessSessionEntryViewModel(entry));

        SessionEntries.AddRange(entryViewModels);
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
        _windowService.ExitFullScreen();
        _messenger.Send(new ApplicationMessage.SidebarVisibility(true));

        SetConfigurationView();
        CleanUpEndingView();
    }

    /// <summary>
    /// Sets the current content view to the configuration view and subscribes to its events.
    /// </summary>
    private void SetConfigurationView()
    {
        IsBlobBackgroundVisible = false;
        
        // initialize configuration view model if it doesn't exist
        if (_configurationViewModel == null)
        {
            _configurationViewModel = new WellnessStartViewModel(
                _wellnessSessionEntryService, _breathingTechniqueService, _dialogService, _startLogger);

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
        IsBlobBackgroundVisible = true;
        
        _windowService.EnterFullScreen();
        _messenger.Send(new ApplicationMessage.SidebarVisibility(false));

        _sessionViewModel = new WellnessSessionViewModel(sessionConfiguration);
        _sessionViewModel.SessionEnded += OnSessionEnded;

        ContentViewModel = _sessionViewModel;
    }

    /// <summary>
    /// Sets the current content view to the ending summary view and subscribes to its events.
    /// </summary>
    /// <param name="createEntryRequest">A create request to pass to the ending view model for saving.</param>
    private void SetEndingView(CreateWellnessSessionEntryRequest createEntryRequest)
    {
        IsBlobBackgroundVisible = true;
        
        _endingViewModel = new WellnessEndingViewModel(_wellnessSessionEntryService, _dialogService, createEntryRequest, _endingLogger);
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