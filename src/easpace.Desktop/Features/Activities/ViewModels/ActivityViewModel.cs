// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Activities.ViewModels;

public abstract partial class ActivityViewModel : ViewModelBase
{
    private readonly IDataEntryService _dataEntryService;
    private readonly Activity _activity;

    public Guid Id { get; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isArchived;

    public AvaloniaList<DataEntryViewModel> Entries { get; } = [];

    protected ActivityViewModel(Activity activity, IDataEntryService dataEntryService)
    {
        _activity = activity;
        _dataEntryService = dataEntryService;

        Id = _activity.Id;
        Name = _activity.Name;

        LoadEntries();
    }

    public abstract Activity? UpdateFrom(UpdateActivityRequest updateRequest);

    private void LoadEntries()
    {
        switch (_activity)
        {
            case NumericActivity numericActivity:

                var numericVms = numericActivity.Entries
                    .OfType<NumericDataEntry>()
                    .Select(e => new NumericDataEntryViewModel(e));

                Entries.Clear();
                Entries.AddRange(numericVms);

                break;

            case RoutineActivity routineActivity:

                var routineVms = routineActivity.Entries
                    .OfType<RoutineDataEntry>()
                    .Select(e => new RoutineDataEntryViewModel(e));

                Entries.Clear();
                Entries.AddRange(routineVms);

                break;

            default:
                throw new NotSupportedException($"Undefined activity type: {_activity.GetType().Name}");
        }
    }

    public DataEntryViewModel? AddDataEntry(CreateDataEntryRequest createRequest)
    {
        var addedEntry = _dataEntryService.CreateDataEntry(_activity.Id, createRequest);

        if (addedEntry is null) return null;

        switch (createRequest.Type)
        {
            case DataEntryType.Numeric:
                if (addedEntry is not NumericDataEntry numericDataEntry) return null;
                var numericEntryVm = new NumericDataEntryViewModel(numericDataEntry);
                Entries.Add(numericEntryVm);
                return numericEntryVm;

            case DataEntryType.Routine:
                if (addedEntry is not RoutineDataEntry routineDataEntry) return null;
                var routineEntryVm = new RoutineDataEntryViewModel(routineDataEntry);
                Entries.Add(routineEntryVm);
                return routineEntryVm;

            default:
                throw new NotSupportedException($"Undefined data entry type: {createRequest.Type}");
        }
    }

    public DataEntryViewModel? EditDataEntry(Guid entryId, UpdateDataEntryRequest updateRequest)
    {
        var entryVm = Entries.FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return null;

        var updatedEntry = _dataEntryService.UpdateDataEntry(entryId, updateRequest);

        if (updatedEntry is null) return null;

        if (updateRequest.Timestamp is not null)
        {
            entryVm.Timestamp = updatedEntry.Timestamp;
        }

        switch (entryVm)
        {
            case NumericDataEntryViewModel numericDataEntryViewModel:

                if (updateRequest.Value is not null)
                {
                    numericDataEntryViewModel.Value = updateRequest.Value.Value;
                }

                return numericDataEntryViewModel;

            case RoutineDataEntryViewModel routineDataEntryViewModel:

                if (updateRequest.State is not null)
                {
                    routineDataEntryViewModel.State = updateRequest.State.Value;
                }

                return routineDataEntryViewModel;

            default:
                throw new NotSupportedException($"Undefined data entry type: {entryVm.GetType().Name}");
        }
    }

    public bool DeleteDataEntry(Guid entryId)
    {
        var entryVm = Entries.FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return false;

        var deleted = _dataEntryService.DeleteDataEntry(entryVm.Id);

        if (!deleted) return false;

        Entries.Remove(entryVm);
        return true;
    }
}