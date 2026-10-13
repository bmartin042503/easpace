// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Repositories;
using easpace.Desktop.Features.Wellness.ViewModels.Exercises;
using easpace.Desktop.Features.Wellness.ViewModels.Session;
using easpace.Desktop.Services.Data;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal partial class WellnessPageViewModel : PageViewModel
{
    private readonly IMessenger _messenger;
    private readonly IWindowService _windowService;
    private readonly IPreferencesService _preferencesService;
    private readonly IWellnessSessionEntryRepository _wellnessSessionEntryRepository;
    private readonly IWellnessExerciseRepository _wellnessExerciseRepository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessConfigurationViewModel> _configurationLogger;
    private readonly ILogger<WellnessExercisesViewModel> _exercisesLogger;

    [ObservableProperty] private ObservableObject? _contentViewModel;
    [ObservableProperty] private bool _isBlobBackgroundVisible;

    private bool _isFullScreenSettingOn;
    private bool _isAnimatedBgSettingOn;

    public WellnessPageViewModel(
        IMessenger messenger,
        IWindowService windowService,
        IPreferencesService preferencesService,
        IWellnessSessionEntryRepository wellnessSessionEntryRepository,
        IWellnessExerciseRepository wellnessExerciseRepository,
        IDialogService dialogService,
        ILogger<WellnessConfigurationViewModel> configurationLogger,
        ILogger<WellnessExercisesViewModel> exercisesLogger)
    {
        Page = ApplicationPage.Wellness;
        _messenger = messenger;
        _windowService = windowService;
        _preferencesService = preferencesService;
        _wellnessSessionEntryRepository = wellnessSessionEntryRepository;
        _wellnessExerciseRepository = wellnessExerciseRepository;
        _dialogService = dialogService;
        _configurationLogger = configurationLogger;
        _exercisesLogger = exercisesLogger;

        LoadWellnessSettings();
        ShowConfiguration();
    }

    [RelayCommand]
    public void Initialize()
    {
        LoadWellnessSettings();
    }

    private void ShowConfiguration()
    {
        var configurationViewModel = new WellnessConfigurationViewModel(
            _wellnessSessionEntryRepository,
            _wellnessExerciseRepository,
            _dialogService,
            _configurationLogger
        );

        configurationViewModel.SessionStarted += OnSessionStarted;
        configurationViewModel.ExercisesViewRequested += OnExercisesViewRequested;

        NavigateTo(configurationViewModel);
    }

    private void ShowExercises()
    {
        var exercisesViewModel = new WellnessExercisesViewModel(
            _wellnessExerciseRepository,
            _dialogService,
            _exercisesLogger
        );
        exercisesViewModel.ConfigurationViewRequested += OnConfigurationViewRequested;
        NavigateTo(exercisesViewModel);
    }

    private void ShowSession(WellnessSessionConfiguration sessionConfiguration)
    {
        var sessionViewModel = new WellnessSessionViewModel(_preferencesService, sessionConfiguration);
        NavigateTo(sessionViewModel);
    }


    private void LoadWellnessSettings()
    {
        _isAnimatedBgSettingOn = _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessAnimatedBackground);
        _isFullScreenSettingOn = _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessFullScreen);
    }

    private void NavigateTo(ObservableObject viewModel)
    {
        CleanUpCurrentView();

        var wasInSession = ContentViewModel is WellnessSessionViewModel;
        var navigatedToSession = viewModel is WellnessSessionViewModel;

        if (wasInSession && !navigatedToSession)
        {
            if (_isFullScreenSettingOn)
            {
                _windowService.ExitFullScreen();
            }

            _messenger.Send(new ApplicationMessage.SidebarVisibility(true));
        }
        else if (!wasInSession && navigatedToSession)
        {
            if (_isFullScreenSettingOn)
            {
                _windowService.EnterFullScreen();
            }

            _messenger.Send(new ApplicationMessage.SidebarVisibility(false));
        }

        IsBlobBackgroundVisible = navigatedToSession && _isAnimatedBgSettingOn;
        ContentViewModel = viewModel;
    }

    private void CleanUpCurrentView()
    {
        if (ContentViewModel is WellnessConfigurationViewModel configurationViewModel)
        {
            configurationViewModel.SessionStarted -= OnSessionStarted;
            configurationViewModel.ExercisesViewRequested -= OnExercisesViewRequested;
        }

        if (ContentViewModel is WellnessExercisesViewModel exercisesViewModel)
        {
            exercisesViewModel.ConfigurationViewRequested -= OnConfigurationViewRequested;
        }

        if (ContentViewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void OnSessionStarted(object? sender, WellnessSessionConfiguration sessionConfiguration)
    {
        ShowSession(sessionConfiguration);
    }

    private void OnExercisesViewRequested(object? sender, EventArgs e)
    {
        ShowExercises();
    }

    private void OnConfigurationViewRequested(object? sender, EventArgs e)
    {
        ShowConfiguration();
    }
}