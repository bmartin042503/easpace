// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;
using easpace.Desktop.Services;

namespace easpace.Desktop.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    [ObservableProperty] private string _versionText = string.Empty;
    
    public SettingsViewModel()
    {
        Page = ApplicationPage.Settings;

        VersionText = string.Format(LocalizationService.GetString("Credits.Text.Version"), App.Version.ToString());
    }
}