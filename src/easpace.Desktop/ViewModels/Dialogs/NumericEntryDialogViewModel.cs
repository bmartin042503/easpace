// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace easpace.Desktop.ViewModels.Dialogs;

public partial class NumericEntryDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _confirmText = string.Empty;
    [ObservableProperty] private string _cancelText = string.Empty;
    [ObservableProperty] private string? _unitText = string.Empty;
    [ObservableProperty] private DateTime? _selectedDate = DateTime.Now;
    [ObservableProperty] private double? _numericValue;
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