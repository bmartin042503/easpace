// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Contracts;

public class RoutineMonth
{
    public int Year { get; set; }
    public int Month { get; set; }
    
    public IEnumerable<RoutineActivityDataEntry> Entries { get; set; } = [];
}