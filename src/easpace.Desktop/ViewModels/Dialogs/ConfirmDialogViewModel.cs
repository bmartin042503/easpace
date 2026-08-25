// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace easpace.Desktop.ViewModels.Dialogs;

internal partial class ConfirmDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _confirmText = string.Empty;
    [ObservableProperty] private string _cancelText = string.Empty;
    [ObservableProperty] private bool _isDestructive;
    
    [ObservableProperty] private bool _confirmed;
    
    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
        Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        Confirmed = false;
        Close();
    }
}