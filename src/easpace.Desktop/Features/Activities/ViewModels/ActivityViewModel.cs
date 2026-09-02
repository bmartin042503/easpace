// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal abstract partial class ActivityViewModel : ViewModelBase
{
    private readonly IActivityDataEntryService _activityDataEntryService;
    private readonly Activity _activity;

    public Guid Id { get; }
    
    public DateTimeOffset CreatedAt { get; }

    public event EventHandler? EditRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? ArchiveToggled;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isArchived;

    public AvaloniaList<ActivityDataEntryViewModel> Entries { get; } = [];

    protected ActivityViewModel(Activity activity, IActivityDataEntryService activityDataEntryService)
    {
        _activity = activity;
        _activityDataEntryService = activityDataEntryService;

        Id = _activity.Id;
        Name = _activity.Name;
        CreatedAt = _activity.CreatedAt;
        IsArchived = _activity.IsArchived;

        Entries.CollectionChanged += (s, e) =>
        {
            OnEntryCollectionChanged();
        };
    }
    
    protected virtual void OnEntryCollectionChanged() {}

    public abstract Task<Activity?> UpdateFrom(UpdateActivityRequest updateRequest);

    public abstract Task<ActivityDataEntryViewModel?> EditDataEntry(Guid entryId);

    [RelayCommand]
    private void ToggleArchive()
    {
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

    protected void LoadEntries()
    {
        switch (_activity)
        {
            case NumericActivity numericActivity:

                var numericVms = numericActivity.Entries
                    .OfType<NumericActivityDataEntry>()
                    .Select(e => new NumericActivityDataEntryViewModel(e));

                Entries.Clear();
                Entries.AddRange(numericVms);

                break;

            case RoutineActivity routineActivity:

                var routineVms = routineActivity.Entries
                    .OfType<RoutineActivityDataEntry>()
                    .Select(e => new RoutineActivityDataEntryViewModel(e));

                Entries.Clear();
                Entries.AddRange(routineVms);

                break;

            default:
                throw new NotSupportedException($"Undefined activity type: {_activity.GetType().Name}");
        }
    }

    public async Task<bool> DeleteDataEntryAsync(Guid entryId)
    {
        var entryVm = Entries.FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return false;

        var deleted = await _activityDataEntryService.DeleteDataEntryAsync(entryVm.Id);

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