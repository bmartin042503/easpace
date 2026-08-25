// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.ViewModels;

internal partial class SettingsViewModel : PageViewModel
{
    private readonly IApplicationService _applicationService;
    private readonly IDataWipeService _dataWipeService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<LegalInfoDialogViewModel> _logger;

    [ObservableProperty] private string _versionText = string.Empty;

    public SettingsViewModel(
        IApplicationService applicationService,
        IDataWipeService dataWipeService,
        IDialogService dialogService,
        ILogger<LegalInfoDialogViewModel> logger)
    {
        Page = ApplicationPage.Settings;

        _applicationService = applicationService;
        _dataWipeService = dataWipeService;
        _dialogService = dialogService;
        _logger = logger;

        VersionText = string.Format(LocalizationService.GetString("Credits.Text.Version"), App.Version.ToString());
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
            Title = string.Format(LocalizationService.GetString("Data.DeleteAllDataDialog.Title")),
            Message = LocalizationService.GetString("Data.DeleteAllDataDialog.Description"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
            IsDestructive = true,
        };

        await _dialogService.ShowDialogAsync(confirmDeletionDialog);

        if (!confirmDeletionDialog.Confirmed) return;

        // wiping all data
        _dataWipeService.DeleteDatabaseFile();
        _dataWipeService.DeleteEncryptionKey();
        _dataWipeService.DeletePreferencesFile();

#if DEBUG
        _applicationService.Shutdown();
#elif RELEASE
        _applicationService.Restart();
#endif
        
    }
}