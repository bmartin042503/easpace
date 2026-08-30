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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetDate))]

    private DateOnly? _targetDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStartDate))]
    private DateOnly? _startDate;
    
    public double EntriesSum => Entries.OfType<NumericActivityDataEntryViewModel>().Sum(entry => entry.Value);
    
    public MilestoneActivityViewModel(
        MilestoneActivity milestoneActivity,
        IActivityDataEntryService activityDataEntryService,
        IActivityService activityService,
        IDialogService dialogService) : base(milestoneActivity, activityDataEntryService, dialogService)
    {
        _activityService = activityService;
        StartDate = milestoneActivity.StartDate;
        TargetDate = milestoneActivity.TargetDate;
        
        LoadEntries();
    }

    public bool HasTargetDate => TargetDate.HasValue && TargetDate.Value != DateOnly.MinValue;
    public bool HasStartDate => StartDate.HasValue && StartDate.Value != DateOnly.MinValue;

    public override async Task<Activity?> UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = await _activityService.UpdateActivityAsync(Id, updateRequest);

        if (updated is not MilestoneActivity milestone)
            return null;

        Name = milestone.Name;
        Unit = milestone.Unit;
        Target = milestone.Target;
        StartDate = milestone.StartDate;
        TargetDate = milestone.TargetDate;

        return milestone;
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