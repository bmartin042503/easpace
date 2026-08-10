// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;

namespace easpace.Desktop.ViewModels.Dialogs;

public partial class RoutineEntryDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _confirmText = string.Empty;
    [ObservableProperty] private string _cancelText = string.Empty;
    [ObservableProperty] private DateTime _selectedDate = DateTime.Now;
    [ObservableProperty] private IEnumerable<RoutineState> _states = Enum.GetValues<RoutineState>();
    [ObservableProperty] private RoutineState _selectedItem;
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