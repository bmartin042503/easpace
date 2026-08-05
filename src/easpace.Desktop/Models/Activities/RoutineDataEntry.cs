// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;

namespace easpace.Desktop.Models.Activities;

public class RoutineDataEntry : DataEntry
{
    public RoutineState State { get; set; } = RoutineState.None;
}