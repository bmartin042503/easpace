// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services.Presentation;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal partial class MilestoneActivityViewModel : NumericActivityViewModel
{
    private readonly IActivityService _activityService;
    [ObservableProperty] private DateTimeOffset? _targetDate;

    [ObservableProperty] private DateTimeOffset? _startDate;
    
    public double EntriesSum => Entries.OfType<NumericActivityDataEntryViewModel>().Sum(entry => entry.Value);
    public bool HasValidTargetDate => TargetDate.HasValue && TargetDate.Value != DateTimeOffset.MinValue;
    
    public MilestoneActivityViewModel(
        MilestoneActivity milestoneActivity,
        IActivityDataEntryService activityDataEntryService,
        IActivityService activityService,
        IDialogService dialogService) : base(milestoneActivity, activityDataEntryService, dialogService)
    {
        _activityService = activityService;
        StartDate = milestoneActivity.CreatedAt;
        TargetDate = milestoneActivity.TargetDate;
        
        LoadEntries();
    }

    public override async Task<Activity?> UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = await _activityService.UpdateActivityAsync(Id, updateRequest);

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

    protected override void OnDataEntryUpdated()
    {
        base.OnDataEntryUpdated();
        OnPropertyChanged(nameof(EntriesSum));
    }
}