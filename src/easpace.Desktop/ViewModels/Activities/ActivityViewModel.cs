// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public abstract partial class ActivityViewModel : ViewModelBase
{
    [ObservableProperty] private Activity _baseActivity = null!;

    // notify UI that the base activity has been changed so it'll be updated for the child class
    partial void OnBaseActivityChanged(Activity value) => OnPropertyChanged(nameof(Activity));

    public event EventHandler? DeleteRequested;
    public event EventHandler? EditRequested;
    
    [RelayCommand]
    private void Delete()
    {
        DeleteRequested?.Invoke(this, EventArgs.Empty);
    }
    
    [RelayCommand]
    private void Edit()
    {
        EditRequested?.Invoke(this, EventArgs.Empty);
    }
}