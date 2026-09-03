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
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal partial class RoutineActivityViewModel : ActivityViewModel
{
    private readonly RoutineActivity _routineActivity;
    private readonly IRoutineActivityDataProvider _routineActivityDataProvider;
    private readonly IActivityService _activityService;
    private readonly IDialogService _dialogService;
    private readonly IActivityDataEntryService _activityDataEntryService;

    public AvaloniaList<RoutineMonth> RoutineMonths { get; } = [];

    public RoutineActivityDataEntryViewModel? TodayEntry =>
        Entries
            .OfType<RoutineActivityDataEntryViewModel>()
            .FirstOrDefault(e => e.Timestamp.Date == DateTime.Today && e.State != RoutineState.None);

    public RoutineActivityViewModel(
        RoutineActivity routineActivity,
        IRoutineActivityDataProvider routineActivityDataProvider,
        IActivityDataEntryService activityDataEntryService,
        IActivityService activityService,
        IDialogService dialogService) : base(routineActivity, activityDataEntryService)
    {
        _routineActivity = routineActivity;
        _routineActivityDataProvider = routineActivityDataProvider;
        _activityService = activityService;
        _dialogService = dialogService;
        _activityDataEntryService = activityDataEntryService;

        LoadEntries();
        LoadRoutineMonths();
    }

    protected override void OnEntryCollectionChanged()
    {
        var currentVmIds = Entries.Select(e => e.Id).ToHashSet();
        var entriesToRemove = _routineActivity.Entries
            .Where(e => !currentVmIds.Contains(e.Id))
            .ToList();

        foreach (var entryToRemove in entriesToRemove)
        {
            _routineActivity.Entries.Remove(entryToRemove);
        }

        base.OnEntryCollectionChanged();
        OnPropertyChanged(nameof(TodayEntry));

        if (RoutineMonths.Count is <= 0 or 1)
        {
            LoadRoutineMonths();
            return;
        }

        UpdateMonths();
    }

    public override async Task<Activity?> UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = await _activityService.UpdateActivityAsync(Id, updateRequest);

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

        if (routineEntryDialog is not { Confirmed: true })
        {
            return;
        }

        var selectedDate = routineEntryDialog.SelectedDate.Value.Date;
        var timestamp = routineEntryDialog.GetTimestamp();

        var existingEntry = _routineActivity.Entries
            .OfType<RoutineActivityDataEntry>()
            .FirstOrDefault(e => e.Timestamp.Date == selectedDate);

        if (existingEntry is not null)
        {
            var updateEntryRequest = new UpdateDataEntryRequest(
                Timestamp: timestamp,
                State: routineEntryDialog.SelectedState,
                Value: null
            );

            var updatedDataEntry =
                await _activityDataEntryService.UpdateDataEntryAsync(
                    existingEntry.Id,
                    updateEntryRequest);

            if (updatedDataEntry is not RoutineActivityDataEntry routineDataEntry)
            {
                return;
            }

            // synchronize the backing entity
            existingEntry.Timestamp = routineDataEntry.Timestamp;
            existingEntry.State = routineDataEntry.State;

            // synchronize the view model
            var dataEntryVm = Entries
                .OfType<RoutineActivityDataEntryViewModel>()
                .FirstOrDefault(e => e.Id == routineDataEntry.Id);

            if (dataEntryVm is not null)
            {
                dataEntryVm.Timestamp = routineDataEntry.Timestamp;
                dataEntryVm.State = routineDataEntry.State;
            }

            // only the affected calendar month needs rebuilding
            var updatedMonth = _routineActivityDataProvider.BuildRoutineMonth(
                routineDataEntry.Timestamp.Year,
                routineDataEntry.Timestamp.Month,
                _routineActivity);

            ReplaceRoutineMonths([updatedMonth]);

            OnPropertyChanged(nameof(TodayEntry));

            return;
        }

        var createEntryRequest = new CreateDataEntryRequest(
            Timestamp: timestamp,
            State: routineEntryDialog.SelectedState,
            Value: null,
            Type: ActivityDataEntryType.Routine
        );

        var dataEntry =
            await _activityDataEntryService.CreateDataEntryAsync(
                Id,
                createEntryRequest);

        if (dataEntry is not RoutineActivityDataEntry newRoutineDataEntry)
        {
            return;
        }

        _routineActivity.Entries.Add(newRoutineDataEntry);

        var newDataEntryVm =
            new RoutineActivityDataEntryViewModel(newRoutineDataEntry);

        Entries.Insert(0, newDataEntryVm);
    }

    public override async Task<ActivityDataEntryViewModel?> EditDataEntry(Guid entryId)
    {
        var entryVm = Entries.OfType<RoutineActivityDataEntryViewModel>().FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return null;

        var routineEntryDialog = new RoutineEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            SelectedState = entryVm.State,
            SelectedDate = entryVm.Timestamp.Date,
            SelectedTime = entryVm.Timestamp.TimeOfDay
        };

        await _dialogService.ShowDialogAsync(routineEntryDialog);

        if (routineEntryDialog is { Confirmed: true })
        {
            var updateRequest = new UpdateDataEntryRequest(
                Timestamp: routineEntryDialog.GetTimestamp(),
                State: routineEntryDialog.SelectedState,
                Value: null
            );

            var updatedEntry = await _activityDataEntryService.UpdateDataEntryAsync(entryId, updateRequest);

            if (updatedEntry is not RoutineActivityDataEntry routineDataEntry) return null;

            if (updateRequest.Timestamp is not null)
            {
                entryVm.Timestamp = routineDataEntry.Timestamp;
            }

            if (updateRequest.State is not null)
            {
                entryVm.State = routineDataEntry.State;
            }

            var month = _routineActivityDataProvider.BuildRoutineMonth(
                routineDataEntry.Timestamp.Year,
                routineDataEntry.Timestamp.Month,
                _routineActivity);

            ReplaceRoutineMonths([month]);
        }

        return entryVm;
    }

    private void ReplaceRoutineMonths(List<RoutineMonth> newRoutineMonths)
    {
        foreach (var routineMonth in newRoutineMonths)
        {
            var monthToReplace =
                RoutineMonths.FirstOrDefault(m => m.Year == routineMonth.Year && m.Month == routineMonth.Month);

            if (monthToReplace is not null)
            {
                var monthToReplaceIndex = RoutineMonths.IndexOf(monthToReplace);
                RoutineMonths.RemoveAt(monthToReplaceIndex);
                RoutineMonths.Insert(monthToReplaceIndex, routineMonth);
            }
        }
    }

    private void UpdateMonths()
    {
        if (RoutineMonths.Count == 0)
        {
            LoadRoutineMonths();
            return;
        }

        var expectedStartDate = _routineActivity.CreatedAt.Date;
        var expectedEndDate = DateTime.Today;

        if (_routineActivity.Entries.Any())
        {
            var firstEntryDate = _routineActivity.Entries.Min(e => e.Timestamp.Date);
            if (firstEntryDate < expectedStartDate) expectedStartDate = firstEntryDate;

            var lastEntryDate = _routineActivity.Entries.Max(e => e.Timestamp.Date);
            if (lastEntryDate > expectedEndDate) expectedEndDate = lastEntryDate;
        }

        var expectedStartMonth = new DateTime(expectedStartDate.Year, expectedStartDate.Month, 1);
        var expectedEndMonth = new DateTime(expectedEndDate.Year, expectedEndDate.Month, 1);

        var currentStartMonthData = RoutineMonths.MinBy(m => new DateTime(m.Year, m.Month, 1));
        var currentEndMonthData = RoutineMonths.MaxBy(m => new DateTime(m.Year, m.Month, 1));

        var currentStartMonth = new DateTime(currentStartMonthData!.Year, currentStartMonthData.Month, 1);
        var currentEndMonth = new DateTime(currentEndMonthData!.Year, currentEndMonthData.Month, 1);

        if (expectedStartMonth != currentStartMonth || expectedEndMonth != currentEndMonth)
        {
            LoadRoutineMonths();
            return;
        }

        var affectedMonthsDates =
            _routineActivityDataProvider.GetAffectedMonths(RoutineMonths.ToList(), _routineActivity);

        List<RoutineMonth> builtAffectedMonths = [];

        foreach (var affectedMonthDate in affectedMonthsDates)
        {
            var builtMonth = _routineActivityDataProvider.BuildRoutineMonth(affectedMonthDate.Year,
                affectedMonthDate.Month, _routineActivity);

            builtAffectedMonths.Add(builtMonth);
        }

        ReplaceRoutineMonths(builtAffectedMonths);
    }
}