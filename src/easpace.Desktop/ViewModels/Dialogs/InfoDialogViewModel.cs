// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace easpace.Desktop.ViewModels.Dialogs;

internal partial class InfoDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    
    [RelayCommand]
    private void CloseDialog()
    {
        Close();
    }
}