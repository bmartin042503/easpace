// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.
 
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Constants;

namespace easpace.Desktop.Features.Activities.ViewModels.Dialogs;

internal partial class RoutineEntryDialogViewModel : EntryDialogViewModel
{
    [ObservableProperty] private IEnumerable<RoutineState> _states = [RoutineState.Completed, RoutineState.NotCompleted];
    [ObservableProperty] private RoutineState _selectedState;
}