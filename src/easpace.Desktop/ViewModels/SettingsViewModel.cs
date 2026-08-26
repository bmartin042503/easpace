// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel;
using System.Threading.Tasks;
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
    private readonly IApplicationService _applicationService;
    private readonly IDataWipeService _dataWipeService;
    private readonly IDialogService _dialogService;
    private readonly IToastMessageService _toastMessageService;
    private readonly ILogger<LegalInfoDialogViewModel> _logger;

    [ObservableProperty] private string _versionText = string.Empty;

    // setting fields
    [ObservableProperty] private int _selectedLanguageIndex;
    [ObservableProperty] private int _selectedColorSchemeIndex;
    [ObservableProperty] private bool _isWellnessFullScreenEnabled;
    [ObservableProperty] private bool _isWellnessAnimatedBackgroundEnabled;
    [ObservableProperty] private bool _isCheckForUpdatesEnabled;

    private bool _isLoading = true;

    public SettingsViewModel(
        IPreferencesService preferencesService,
        IApplicationService applicationService,
        IDataWipeService dataWipeService,
        IDialogService dialogService,
        IToastMessageService toastMessageService,
        ILogger<LegalInfoDialogViewModel> logger)
    {
        Page = ApplicationPage.Settings;

        _preferencesService = preferencesService;
        _applicationService = applicationService;
        _dataWipeService = dataWipeService;
        _dialogService = dialogService;
        _toastMessageService = toastMessageService;
        _logger = logger;

        VersionText = string.Format(LocalizationService.GetString("Credits.Text.Version"), App.Version.ToString());

        LoadSettings();

        PropertyChanged += OnSettingChanged;
    }

    [RelayCommand]
    private async Task ShowLegalFile(object parameter)
    {
        if (parameter is not LegalFileType legalFileType) return;

        var legalInfoDialog = new LegalInfoDialogViewModel(_logger, legalFileType);

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
            IsDestructive = true,
            IsCritical = true,
        };

        await _dialogService.ShowDialogAsync(confirmDeletionDialog);

        if (!confirmDeletionDialog.Confirmed) return;

        // wiping all data
        _dataWipeService.DeleteDatabaseFile();
        _dataWipeService.DeleteEncryptionKey();
        _dataWipeService.DeletePreferencesFile();

        _applicationService.Restart();
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

        var colorScheme = _preferencesService.ReadPreference<string>(PreferenceKey.ColorScheme);
        SelectedColorSchemeIndex = colorScheme switch
        {
            "system" => 0,
            "light" => 1,
            "dark" => 2,
            _ => 0
        };

        IsWellnessFullScreenEnabled = _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessFullScreen);
        
        IsWellnessAnimatedBackgroundEnabled =
            _preferencesService.ReadPreference<bool>(PreferenceKey.WellnessAnimatedBackground);
        
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
            0 => "system",
            1 => "light",
            2 => "dark",
            _ => string.Empty
        };
        
        _preferencesService.SavePreference(PreferenceKey.Language, language);
        _preferencesService.SavePreference(PreferenceKey.ColorScheme, colorScheme);
        _preferencesService.SavePreference(PreferenceKey.WellnessFullScreen, IsWellnessFullScreenEnabled);
        _preferencesService.SavePreference(PreferenceKey.WellnessAnimatedBackground, IsWellnessAnimatedBackgroundEnabled);
        _preferencesService.SavePreference(PreferenceKey.CheckForUpdates, IsCheckForUpdatesEnabled);
        
        // restart required
        if (previousLanguage != language)
        {
            var restartConfirmDialog = new ConfirmDialogViewModel
            {
                Title = string.Format(LocalizationService.GetString("Settings.RestartDialog.Title")),
                Message = string.Format(LocalizationService.GetString("Settings.RestartDialog.Description")),
                CancelText = LocalizationService.GetString("Settings.RestartDialog.Later"),
                ConfirmText = LocalizationService.GetString("Settings.RestartDialog.RestartNow"),
            };
            
            await _dialogService.ShowDialogAsync(restartConfirmDialog);

            if (restartConfirmDialog.Confirmed)
            {
                _applicationService.Restart();
            }
        }
        
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
            nameof(IsWellnessFullScreenEnabled) or 
            nameof(IsWellnessAnimatedBackgroundEnabled) or 
            nameof(IsCheckForUpdatesEnabled))
        {
            await SaveSettings();
        }
    }
}