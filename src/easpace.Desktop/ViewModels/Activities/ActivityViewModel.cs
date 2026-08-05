// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public partial class ActivityViewModel : ViewModelBase
{
    [ObservableProperty] private Activity _baseActivity;

    partial void OnBaseActivityChanged(Activity value) => OnPropertyChanged(nameof(Activity));
}