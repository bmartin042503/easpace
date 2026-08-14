// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;

namespace easpace.Desktop.ViewModels;

public partial class WellnessStartViewModel : ViewModelBase
{
    public event EventHandler<WellnessSessionType>? SessionSelected;

    [RelayCommand]
    private void SelectSession(object? parameter)
    {
        if (parameter is not WellnessSessionType sessionType) return;
        
        SessionSelected?.Invoke(this, sessionType);
    }
}