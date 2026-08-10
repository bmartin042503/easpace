// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public partial class ActivityViewModel : ViewModelBase
{
    [ObservableProperty] private Activity _baseActivity;

    // notify UI that the base activity has been changed so it'll be updated for the child class
    partial void OnBaseActivityChanged(Activity value) => OnPropertyChanged(nameof(Activity));
}