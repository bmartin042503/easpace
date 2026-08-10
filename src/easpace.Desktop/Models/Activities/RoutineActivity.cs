// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models.Activities;

public class RoutineActivity : Activity<RoutineDataEntry>
{
    public RoutineDataEntry? TodayEntry =>
        Entries.FirstOrDefault(e => e.Timestamp.Date == DateTime.Today && e.State != RoutineState.None);

    protected override void OnCollectionChanged()
    {
        base.OnCollectionChanged();

        OnPropertyChanged(nameof(TodayEntry));
    }
}