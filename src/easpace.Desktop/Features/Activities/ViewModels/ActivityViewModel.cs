// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Features.Activities.ViewModels.Dialogs;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Activities.ViewModels;

public abstract partial class ActivityViewModel : ViewModelBase
{
    private readonly IDataEntryService _dataEntryService;
    private readonly Activity _activity;

    public Guid Id { get; }

    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? ArchiveToggled;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isArchived;

    public AvaloniaList<DataEntryViewModel> Entries { get; } = [];

    protected ActivityViewModel(Activity activity, IDataEntryService dataEntryService)
    {
        _activity = activity;
        _dataEntryService = dataEntryService;

        Id = _activity.Id;
        Name = _activity.Name;

        Entries.CollectionChanged += (s, e) =>
        {
            OnEntryCollectionChanged();
        };

        LoadEntries();
    }
    
    protected virtual void OnEntryCollectionChanged() {}

    public abstract Activity? UpdateFrom(UpdateActivityRequest updateRequest);

    public abstract Task<DataEntryViewModel?> EditDataEntry(Guid entryId);

    [RelayCommand]
    private void ToggleArchive()
    {
        IsArchived = !IsArchived;
        ArchiveToggled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Delete()
    {
        DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Edit()
    {
        EditRequested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnIsArchivedChanged(bool value)
    {
        _activity.IsArchived = value;
    }

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

    public bool DeleteDataEntry(Guid entryId)
    {
        var entryVm = Entries.FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return false;

        var deleted = _dataEntryService.DeleteDataEntry(entryVm.Id);

        if (!deleted) return false;

        var entry = _activity.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry != null)
        {
            _activity.Entries.Remove(entry);
        }

        Entries.Remove(entryVm);
        return true;
    }
}