// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels.Exercises;

internal partial class WellnessExercisesViewModel : ViewModelBase
{
    public event EventHandler? ConfigurationViewRequested;

    [RelayCommand]
    private void NavigateBack()
    {
        ConfigurationViewRequested?.Invoke(this, EventArgs.Empty);
    }
}