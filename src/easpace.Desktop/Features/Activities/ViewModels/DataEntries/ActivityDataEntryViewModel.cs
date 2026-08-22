// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Activities.ViewModels.DataEntries;

public abstract partial class ActivityDataEntryViewModel : ViewModelBase
{
    public Guid Id { get; }
    public Guid ActivityId { get; }

    [ObservableProperty] private DateTimeOffset _timestamp;
    
    protected ActivityDataEntryViewModel(ActivityDataEntry activityDataEntry)
    {
        Id = activityDataEntry.Id;
        ActivityId = activityDataEntry.ActivityId;
        Timestamp = activityDataEntry.Timestamp;
    }
}