// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class MilestoneActivityViewModel : NumericActivityViewModel
{
    private readonly IActivityService _activityService;
    [ObservableProperty] private DateTimeOffset? _targetDate;

    [ObservableProperty] private DateTimeOffset? _startDate;
    
    public double EntriesSum => Entries.OfType<NumericDataEntryViewModel>().Sum(entry => entry.Value);
    public bool HasValidTargetDate => TargetDate.HasValue && TargetDate.Value != DateTimeOffset.MinValue;
    
    public MilestoneActivityViewModel(
        MilestoneActivity milestoneActivity,
        IDataEntryService dataEntryService,
        IActivityService activityService,
        IDialogService dialogService) : base(milestoneActivity, dataEntryService, dialogService)
    {
        _activityService = activityService;
        StartDate = milestoneActivity.CreatedAt;
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

    protected override void OnEntryCollectionChanged()
    {
        base.OnEntryCollectionChanged();
        OnPropertyChanged(nameof(EntriesSum));
    }
}