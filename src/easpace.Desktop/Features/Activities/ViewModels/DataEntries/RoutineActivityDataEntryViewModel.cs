// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.ViewModels.DataEntries;

internal partial class RoutineActivityDataEntryViewModel : ActivityDataEntryViewModel
{
    [ObservableProperty] private RoutineState _state;
    
    public RoutineActivityDataEntryViewModel(RoutineActivityDataEntry routineActivityEntry) : base(routineActivityEntry)
    {
        State = routineActivityEntry.State;
    }
}