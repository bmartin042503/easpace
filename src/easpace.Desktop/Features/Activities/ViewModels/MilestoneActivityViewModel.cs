// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class MilestoneActivityViewModel : NumericActivityViewModel
{
    private readonly IActivityService _activityService;
    [ObservableProperty] private DateTimeOffset? _targetDate;
    
    public MilestoneActivityViewModel(
        MilestoneActivity milestoneActivity,
        IDataEntryService dataEntryService,
        IActivityService activityService) : base(milestoneActivity, dataEntryService)
    {
        _activityService = activityService;
        TargetDate = milestoneActivity.TargetDate;
    }

    public override Activity? UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = _activityService.UpdateActivity(Id, updateRequest);

        if (updated is null) return null;
        
        Name = updated.Name;
        Unit = ((MilestoneActivity)updated).Unit;
        Target = ((MilestoneActivity)updated).Target;
        TargetDate = ((MilestoneActivity)updated).TargetDate;
        
        return updated;
    }
}