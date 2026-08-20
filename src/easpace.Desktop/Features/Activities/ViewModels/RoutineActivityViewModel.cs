// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Features.Activities.ViewModels.Dialogs;
using easpace.Desktop.Services;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class RoutineActivityViewModel : ActivityViewModel
{
    private readonly RoutineActivity _routineActivity;
    private readonly IRoutineActivityDataProvider _routineActivityDataProvider;
    private readonly IActivityService _activityService;
    private readonly IDialogService _dialogService;
    private readonly IDataEntryService _dataEntryService;

    public AvaloniaList<RoutineMonth> RoutineMonths { get; } = [];

    public RoutineDataEntryViewModel? TodayEntry =>
        Entries
            .OfType<RoutineDataEntryViewModel>()
            .FirstOrDefault(e => e.Timestamp.Date == DateTime.Today && e.State != RoutineState.None);

    public RoutineActivityViewModel(
        RoutineActivity routineActivity,
        IRoutineActivityDataProvider routineActivityDataProvider,
        IDataEntryService dataEntryService,
        IActivityService activityService,
        IDialogService dialogService) : base(routineActivity, dataEntryService)
    {
        _routineActivity = routineActivity;
        _routineActivityDataProvider = routineActivityDataProvider;
        _activityService = activityService;
        _dialogService = dialogService;
        _dataEntryService = dataEntryService;

        LoadRoutineMonths();
    }

    protected override void OnEntryCollectionChanged()
    {
        base.OnEntryCollectionChanged();
        OnPropertyChanged(nameof(TodayEntry));
        LoadRoutineMonths();
    }

    public override Activity? UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = _activityService.UpdateActivity(Id, updateRequest);

        if (updated is null) return null;

        Name = updated.Name;

        return updated;
    }

    private void LoadRoutineMonths()
    {
        var months = _routineActivityDataProvider.GetRoutineMonths(_routineActivity);

        RoutineMonths.Clear();
        RoutineMonths.AddRange(months);
    }
    
    [RelayCommand]
    private async Task AddDataEntry()
    {
        var routineEntryDialog = new RoutineEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            SelectedDate = DateTime.Now,
            SelectedState = RoutineState.Completed
        };

        await _dialogService.ShowDialogAsync(routineEntryDialog);

        if (routineEntryDialog is { Confirmed: true })
        {
            var createEntryRequest = new CreateDataEntryRequest(
                Timestamp: routineEntryDialog.GetTimestamp(),
                State: routineEntryDialog.SelectedState,
                Value: null,
                Type: DataEntryType.Routine
            );
            
            var dataEntry = _dataEntryService.CreateDataEntry(Id, createEntryRequest);

            if (dataEntry is not RoutineDataEntry routineDataEntry) return;
            
            _routineActivity.Entries.Add(routineDataEntry);
            
            var dataEntryVm = new RoutineDataEntryViewModel(routineDataEntry);
            
            Entries.Add(dataEntryVm);
        }
    }
    
    public override async Task<DataEntryViewModel?> EditDataEntry(Guid entryId)
    {
        var entryVm = Entries.OfType<RoutineDataEntryViewModel>().FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return null;

        var routineEntryDialog = new RoutineEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            SelectedState = entryVm.State,
            SelectedDate = entryVm.Timestamp.Date
        };

        await _dialogService.ShowDialogAsync(routineEntryDialog);

        if (routineEntryDialog is { Confirmed: true })
        {
            var updateRequest = new UpdateDataEntryRequest(
                Timestamp: routineEntryDialog.GetTimestamp(),
                State: routineEntryDialog.SelectedState,
                Value: null
            );

            var updatedEntry = _dataEntryService.UpdateDataEntry(entryId, updateRequest);

            if (updatedEntry is not RoutineDataEntry routineDataEntry) return null;

            if (updateRequest.Timestamp is not null)
            {
                entryVm.Timestamp = routineDataEntry.Timestamp;
            }

            if (updateRequest.State is not null)
            {
                entryVm.State = routineDataEntry.State;
            }
        }

        return entryVm;
    }
}