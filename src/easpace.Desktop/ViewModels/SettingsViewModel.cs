// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.ViewModels;

internal partial class SettingsViewModel : PageViewModel
{
    private readonly IPreferencesService _preferencesService;
    private readonly IColorSchemeService _colorSchemeService;
    private readonly IApplicationService _applicationService;
    private readonly IDataWipeService _dataWipeService;
    private readonly IDialogService _dialogService;
    private readonly IToastMessageService _toastMessageService;

    [ObservableProperty] private string _versionText = string.Empty;

    // setting fields
    [ObservableProperty] private int _selectedLanguageIndex;
    [ObservableProperty] private int _selectedColorSchemeIndex;
    [ObservableProperty] private int _selectedColorSchemeAppearanceIndex;
    [ObservableProperty] private bool _isTransparentWindowEnabled;
    [ObservableProperty] private bool _isWellnessFullScreenEnabled;
    [ObservableProperty] private bool _isWellnessAnimatedBackgroundEnabled;
    [ObservableProperty] private bool _showWellnessTimer;
    [ObservableProperty] private bool _isCheckForUpdatesEnabled;

    private bool _isLoading = true;

    public SettingsViewModel(
        IPreferencesService preferencesService,
        IColorSchemeService colorSchemeService,
        IApplicationService applicationService,
        IDataWipeService dataWipeService,
        IDialogService dialogService,
        IToastMessageService toastMessageService)
    {
        Page = ApplicationPage.Settings;

        _preferencesService = preferencesService;
        _colorSchemeService = colorSchemeService;
        _applicationService = applicationService;
        _dataWipeService = dataWipeService;
        _dialogService = dialogService;
        _toastMessageService = toastMessageService;

        VersionText =
            $"{LocalizationService.GetString("Credits.Text.Version")}: {App.VersionName} (v{App.Version.ToString()})";

        LoadSettings();

        PropertyChanged += OnSettingChanged;
    }

    [RelayCommand]
    private async Task ShowLegalFile(object parameter)
    {
        if (parameter is not LegalFileType legalFileType) return;

        var legalInfoDialog = new LegalInfoDialogViewModel(_applicationService, legalFileType);

        await _dialogService.ShowDialogAsync(legalInfoDialog);
    }

    [RelayCommand]
    private async Task DeleteAllData()
    {
        var confirmDeletionDialog = new ConfirmDialogViewModel
        {
            Title = string.Format(LocalizationService.GetString("Settings.DeleteAllDataDialog.Title")),
            Message = LocalizationService.GetString("Settings.DeleteAllDataDialog.Description"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
            IsDestructive = true
        };

        await _dialogService.ShowDialogAsync(confirmDeletionDialog);

        if (!confirmDeletionDialog.Confirmed) return;

        // wiping all data
        _dataWipeService.DeleteDatabaseFile();
        _dataWipeService.DeleteEncryptionKey();
        _dataWipeService.DeletePreferencesFile();

        // shutdown, so we don't recreate the deleted files if we'd restart
        _applicationService.Shutdown();
    }

    [RelayCommand]
    private async Task OpenGitHubAsync()
    {
        await _applicationService.LaunchUriAsync(new Uri("https://github.com/bmartin042503/easpace"));
    }

    [RelayCommand]
    private async Task OpenLogoCreatorPageAsync()
    {
        await _applicationService.LaunchUriAsync(new Uri("https://www.fiverr.com/l_nuge"));
    }

    private void LoadSettings()
    {
        _isLoading = true;

        var language = _preferencesService.ReadPreference<string>(PreferenceKey.Language);
        SelectedLanguageIndex = language switch
        {
            "en" => 0,
            "hu" => 1,
            _ => 0
        };

        var colorScheme = _preferencesService.ReadPreference<ColorScheme>(PreferenceKey.ColorScheme);
        SelectedColorSchemeIndex = colorScheme switch
        {
            ColorScheme.HavenBlue => 0,
            ColorScheme.AvallamaPurple => 1,
            _ => 0
        };

        var colorSchemeAppearance =
            _preferencesService.ReadPreference<ColorSchemeAppearance>(PreferenceKey.ColorSchemeAppearance);
        SelectedColorSchemeAppearanceIndex = colorSchemeAppearance switch
        {
            ColorSchemeAppearance.Default => 0,
            ColorSchemeAppearance.Light => 1,
            ColorSchemeAppearance.Dark => 2,
            _ => 0
        };

        IsTransparentWindowEnabled = _preferencesService.ReadPreference<bool>(PreferenceKey.TransparentWindow);

        IsWellnessFullScreenEnabled = _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessFullScreen);

        IsWellnessAnimatedBackgroundEnabled =
            _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessAnimatedBackground);
        
        ShowWellnessTimer =
            _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessShowTimer);

        IsCheckForUpdatesEnabled = _preferencesService.ReadPreference<bool>(PreferenceKey.CheckForUpdates);

        _isLoading = false;
    }

    private async Task SaveSettings()
    {
        var previousLanguage = _preferencesService.ReadPreference<string>(PreferenceKey.Language);

        var language = SelectedLanguageIndex switch
        {
            0 => "en",
            1 => "hu",
            _ => string.Empty
        };

        var colorScheme = SelectedColorSchemeIndex switch
        {
            0 => ColorScheme.HavenBlue,
            1 => ColorScheme.AvallamaPurple,
            _ => ColorScheme.HavenBlue
        };

        var colorSchemeAppearance = SelectedColorSchemeAppearanceIndex switch
        {
            0 => ColorSchemeAppearance.Default,
            1 => ColorSchemeAppearance.Light,
            2 => ColorSchemeAppearance.Dark,
            _ => ColorSchemeAppearance.Default
        };

        _preferencesService.SavePreference(PreferenceKey.Language, language);
        _preferencesService.SavePreference(PreferenceKey.ColorScheme, colorScheme);
        _preferencesService.SavePreference(PreferenceKey.ColorSchemeAppearance, colorSchemeAppearance);
        _preferencesService.SavePreference(PreferenceKey.TransparentWindow, IsTransparentWindowEnabled);
        _preferencesService.SavePreference(PreferenceKey.WellnessFullScreen, IsWellnessFullScreenEnabled);
        _preferencesService.SavePreference(PreferenceKey.WellnessAnimatedBackground,
            IsWellnessAnimatedBackgroundEnabled);
        _preferencesService.SavePreference(PreferenceKey.WellnessShowTimer, ShowWellnessTimer);
        _preferencesService.SavePreference(PreferenceKey.CheckForUpdates, IsCheckForUpdatesEnabled);

        // restart required dialog when the language setting has changed
        if (previousLanguage != language)
        {
            var restartConfirmDialog = new ConfirmDialogViewModel
            {
                Title = string.Format(LocalizationService.GetString("Settings.RestartDialog.Title")),
                Message = string.Format(LocalizationService.GetString("Settings.RestartDialog.Description")),
                CancelText = LocalizationService.GetString("Common.Button.Later"),
                ConfirmText = LocalizationService.GetString("Settings.RestartDialog.RestartNow"),
            };

            await _dialogService.ShowDialogAsync(restartConfirmDialog);

            if (restartConfirmDialog.Confirmed)
            {
                _applicationService.Restart();
            }
        }

        _colorSchemeService.SetColorScheme(colorScheme, colorSchemeAppearance);

        _toastMessageService.ShowToastMessage(
            LocalizationService.GetString("Settings.ToastMessage.SettingsSaved"),
            ToastMessageType.Success);
    }

    private async void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoading) return;

        if (e.PropertyName is
            nameof(SelectedLanguageIndex) or
            nameof(SelectedColorSchemeIndex) or
            nameof(SelectedColorSchemeAppearanceIndex) or
            nameof(IsTransparentWindowEnabled) or
            nameof(IsWellnessFullScreenEnabled) or
            nameof(IsWellnessAnimatedBackgroundEnabled) or
            nameof(ShowWellnessTimer) or
            nameof(IsCheckForUpdatesEnabled))
        {
            await SaveSettings();
        }
    }
}