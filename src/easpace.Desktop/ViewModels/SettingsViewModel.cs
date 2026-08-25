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

public partial class SettingsViewModel : PageViewModel
{
    private readonly IDialogService _dialogService;
    private readonly ILogger<LegalInfoDialogViewModel> _logger;
    
    [ObservableProperty] private string _versionText = string.Empty;
    
    public SettingsViewModel(IDialogService dialogService, ILogger<LegalInfoDialogViewModel> logger)
    {
        Page = ApplicationPage.Settings;
        
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
        
    }
}