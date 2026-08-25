// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.Constants;

namespace easpace.Desktop.Features.Activities.Entities.DataEntries;

internal class RoutineActivityDataEntry : ActivityDataEntry
{
    public RoutineState State { get; set; }
}