// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Repositories;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services.Presentation;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal partial class MilestoneActivityViewModel : NumericActivityViewModel
{
    private readonly IActivityRepository _activityRepository;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetDate))]

    private DateOnly? _targetDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStartDate))]
    private DateOnly? _startDate;
    
    public double EntriesSum => Entries.OfType<NumericActivityDataEntryViewModel>().Sum(entry => entry.Value);
    
    public MilestoneActivityViewModel(
        MilestoneActivity milestoneActivity,
        IActivityDataEntryRepository activityDataEntryRepository,
        IActivityRepository activityRepository,
        IDialogService dialogService,
        ILogger<ActivityViewModel> logger) : base(milestoneActivity, activityDataEntryRepository, dialogService, logger)
    {
        _activityRepository = activityRepository;
        StartDate = milestoneActivity.StartDate;
        TargetDate = milestoneActivity.TargetDate;
        
        LoadEntries();
    }

    public bool HasTargetDate => TargetDate.HasValue && TargetDate.Value != DateOnly.MinValue;
    public bool HasStartDate => StartDate.HasValue && StartDate.Value != DateOnly.MinValue;

    public override async Task<Activity?> UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = await _activityRepository.UpdateActivityAsync(Id, updateRequest);

        if (updated is not MilestoneActivity milestoneActivity)
            return null;

        Name = milestoneActivity.Name;
        Unit = milestoneActivity.Unit;
        Target = milestoneActivity.Target;
        StartDate = milestoneActivity.StartDate;
        TargetDate = milestoneActivity.TargetDate;

        return milestoneActivity;
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