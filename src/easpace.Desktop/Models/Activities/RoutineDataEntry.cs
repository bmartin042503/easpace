// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models.Activities;

public partial class RoutineDataEntry : DataEntry
{
    [ObservableProperty] private RoutineState _state = RoutineState.None;
}