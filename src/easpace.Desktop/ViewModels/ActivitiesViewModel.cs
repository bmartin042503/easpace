// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Activities;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels;

public partial class ActivitiesViewModel : PageViewModel
{
    private readonly IDialogService _dialogService;
    public ObservableCollection<ActivityViewModel> ActivityViewModels { get; } = [];
    public ObservableCollection<DataEntry> EditActivityDataEntries { get; } = [];

    [ObservableProperty] private ActivityViewModel? _selectedActivityViewModel;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isCreatingNew;
    [ObservableProperty] private bool _isComboBoxEnabled;

    [NotifyPropertyChangedFor(nameof(IsRoutineSelected))]
    [NotifyPropertyChangedFor(nameof(IsUnitVisible))]
    [NotifyPropertyChangedFor(nameof(IsTargetCheckboxVisible))]
    [NotifyPropertyChangedFor(nameof(IsTargetInputVisible))]
    [NotifyPropertyChangedFor(nameof(IsTargetDateVisible))]
    [ObservableProperty]
    private ActivityType? _editSelectedActivityType;

    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editUnit = string.Empty;

    [NotifyPropertyChangedFor(nameof(IsTargetInputVisible))] [ObservableProperty]
    private bool _isTargetChecked;

    [ObservableProperty] private double? _editTarget;
    [ObservableProperty] private DateTime? _editTargetDate;

    public IEnumerable<ActivityType> ActivityTypes { get; } = Enum.GetValues<ActivityType>();

    public bool IsRoutineSelected => EditSelectedActivityType is ActivityType.Routine;
    public bool IsUnitVisible => EditSelectedActivityType is ActivityType.Trend or ActivityType.Milestone;
    public bool IsTargetCheckboxVisible => EditSelectedActivityType is ActivityType.Trend;
    public bool HasActivities => ActivityViewModels.Count > 0;
    public bool IsDataEntriesVisible => EditActivityDataEntries.Count > 0 && !IsCreatingNew;

    public string DataEntriesLabel =>
        EditActivityDataEntries.Count == 1
            ? LocalizationService.GetString("Activities.Label.DataEntry")
            : string.Format(LocalizationService.GetString("Activities.Label.DataEntries"),
                EditActivityDataEntries.Count);

    public bool IsTargetInputVisible =>
        EditSelectedActivityType is ActivityType.Milestone ||
        (EditSelectedActivityType is ActivityType.Trend && IsTargetChecked);

    public bool IsTargetDateVisible => EditSelectedActivityType is ActivityType.Milestone;

    public ActivitiesViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;

        Page = ApplicationPage.Activities;

        ActivityViewModels.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasActivities));

        EditActivityDataEntries.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(IsDataEntriesVisible));
            OnPropertyChanged(nameof(DataEntriesLabel));
        };

        if (ActivityViewModels.Any())
        {
            SelectedActivityViewModel = ActivityViewModels.First();
        }
    }

    [RelayCommand]
    private async Task AddNewDataEntry()
    {
        if (SelectedActivityViewModel == null) return;

        if (SelectedActivityViewModel.BaseActivity is NumericActivity numericActivity)
        {
            var numericEntryDialog = new NumericEntryDialogViewModel
            {
                Title = LocalizationService.GetString("Activities.EntryDialog.Title"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Save"),
                UnitText = string.IsNullOrEmpty(numericActivity.Unit) ? null : numericActivity.Unit,
                SelectedDate = DateTime.Now
            };

            await _dialogService.ShowDialogAsync(numericEntryDialog);

            if (numericEntryDialog is { Confirmed: true, NumericValue: not null })
            {
                var date = numericEntryDialog.SelectedDate;
                var numericValue = numericEntryDialog.NumericValue.Value;
                var numericEntry = new NumericDataEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = date ?? DateTime.Now,
                    Value = numericValue
                };
                numericActivity.Entries.Add(numericEntry);

                // Keep the edit view's list in sync if it's currently open
                if (IsEditing) EditActivityDataEntries.Add(numericEntry);
            }
        }
        else if (SelectedActivityViewModel.BaseActivity is RoutineActivity routineActivity)
        {
            var routineEntryDialog = new RoutineEntryDialogViewModel
            {
                Title = LocalizationService.GetString("Activities.EntryDialog.Title"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Save"),
                SelectedDate = DateTime.Now,
                SelectedItem = RoutineState.Completed
            };

            await _dialogService.ShowDialogAsync(routineEntryDialog);

            if (routineEntryDialog.Confirmed)
            {
                var date = routineEntryDialog.SelectedDate;
                var state = routineEntryDialog.SelectedItem;

                var existingEntry = routineActivity.Entries.FirstOrDefault(e => e.Timestamp.Date == date.Date);

                if (existingEntry != null)
                {
                    if (state is RoutineState.None)
                    {
                        routineActivity.Entries.Remove(existingEntry);
                        if (IsEditing) EditActivityDataEntries.Remove(existingEntry);
                        return;
                    }

                    var newEntry = new RoutineDataEntry
                    {
                        Id = existingEntry.Id,
                        Timestamp = date,
                        State = state
                    };

                    ReplaceEntry(routineActivity.Entries, existingEntry, newEntry);
                    if (IsEditing) ReplaceEntry(EditActivityDataEntries, existingEntry, newEntry);
                }
                else
                {
                    if (state is RoutineState.None) return;

                    var routineEntry = new RoutineDataEntry
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = date,
                        State = state
                    };
                    routineActivity.Entries.Add(routineEntry);
                    if (IsEditing) EditActivityDataEntries.Add(routineEntry);
                }
            }
        }
    }

    [RelayCommand]
    private void AddNewActivity()
    {
        EditName = LocalizationService.GetString("Activities.Input.NewActivityName");
        EditTitle = LocalizationService.GetString("Activities.Input.NewActivityName");

        IsEditing = true;
        IsCreatingNew = true;

        IsComboBoxEnabled = true;
        EditSelectedActivityType = ActivityTypes.FirstOrDefault();

        EditUnit = string.Empty;
        EditTarget = null;
        EditTargetDate = null;
        IsTargetChecked = false;
        EditActivityDataEntries.Clear();
    }

    [RelayCommand]
    private void EditActivity()
    {
        if (SelectedActivityViewModel == null) return;

        IsEditing = true;
        IsCreatingNew = false;
        IsComboBoxEnabled = false;

        var activity = SelectedActivityViewModel.BaseActivity;
        IEnumerable<DataEntry> dataEntries = [];
        EditName = activity.Title;
        EditTitle = string.Format(LocalizationService.GetString("Activities.Title.Edit"), activity.Title);

        switch (activity)
        {
            case TrendActivity trendActivity:
                EditSelectedActivityType = ActivityType.Trend;
                EditUnit = trendActivity.Unit ?? string.Empty;
                EditTargetDate = null;

                if (trendActivity.Target.HasValue)
                {
                    IsTargetChecked = true;
                    EditTarget = trendActivity.Target.Value;
                }
                else
                {
                    IsTargetChecked = false;
                    EditTarget = null;
                }

                dataEntries = trendActivity.Entries;
                break;

            case MilestoneActivity milestoneActivity:
                EditSelectedActivityType = ActivityType.Milestone;
                EditUnit = milestoneActivity.Unit ?? string.Empty;

                if (milestoneActivity.Target.HasValue)
                {
                    EditTarget = milestoneActivity.Target.Value;
                }

                EditTargetDate = milestoneActivity.TargetDate;

                dataEntries = milestoneActivity.Entries;
                break;

            case RoutineActivity routineActivity:
                EditSelectedActivityType = ActivityType.Routine;
                EditUnit = string.Empty;
                EditTarget = null;
                IsTargetChecked = false;

                dataEntries = routineActivity.Entries;
                break;
        }

        EditActivityDataEntries.Clear();

        // Populate the edit list
        foreach (var dataEntry in dataEntries.OrderByDescending(e => e.Timestamp))
        {
            EditActivityDataEntries.Add(dataEntry);
        }
    }

    [RelayCommand]
    private void SaveActivity()
    {
        if (!IsEditing) return;
        if (string.IsNullOrEmpty(EditName)) return;

        if (IsCreatingNew)
        {
            var vm = new ActivityViewModel();
            switch (EditSelectedActivityType)
            {
                case ActivityType.Trend:
                    var trendActivity = new TrendActivity
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.Now,
                        Title = EditName,
                        Unit = EditUnit
                    };

                    if (IsTargetChecked && EditTarget.HasValue)
                    {
                        trendActivity.Target = EditTarget;
                    }

                    vm = new TrendActivityViewModel(trendActivity);
                    break;

                case ActivityType.Milestone:
                    if (!EditTarget.HasValue) return;

                    var milestoneActivity = new MilestoneActivity
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.Now,
                        Title = EditName,
                        Unit = EditUnit,
                        Target = EditTarget,
                    };

                    if (EditTargetDate != null && EditTargetDate >= DateTime.Now)
                    {
                        milestoneActivity.TargetDate = EditTargetDate;
                    }

                    vm = new MilestoneActivityViewModel(milestoneActivity);
                    break;

                case ActivityType.Routine:
                    var routineActivity = new RoutineActivity
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.Now,
                        Title = EditName
                    };

                    vm = new RoutineActivityViewModel(routineActivity);
                    break;
            }

            ActivityViewModels.Insert(0, vm);
            SelectedActivityViewModel = vm;
        }
        else
        {
            if (SelectedActivityViewModel == null) return;

            // Only metadata is saved here. DataEntries are managed immediately upon edit/delete.
            var activity = SelectedActivityViewModel.BaseActivity;
            activity.Title = EditName;

            switch (activity)
            {
                case TrendActivity trendActivity:
                    trendActivity.Unit = EditUnit;
                    trendActivity.Target = IsTargetChecked ? EditTarget : null;
                    break;

                case MilestoneActivity milestoneActivity:
                    if (!EditTarget.HasValue) return;

                    milestoneActivity.Unit = EditUnit;
                    milestoneActivity.Target = EditTarget;

                    if (EditTargetDate != null && EditTargetDate >= DateTime.Now)
                    {
                        milestoneActivity.TargetDate = EditTargetDate;
                    }

                    break;
            }
        }

        IsEditing = false;
        IsCreatingNew = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsEditing = false;
        IsCreatingNew = false;
    }

    [RelayCommand]
    private async Task DeleteActivity()
    {
        if (SelectedActivityViewModel == null) return;

        var confirmDeletionDialog = new ConfirmDialogViewModel
        {
            Title = string.Format(LocalizationService.GetString("Activities.DeleteDialog.Title"),
                SelectedActivityViewModel.BaseActivity.Title),
            Message = LocalizationService.GetString("Activities.DeleteDialog.Message"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
            IsDestructive = true,
        };

        await _dialogService.ShowDialogAsync(confirmDeletionDialog);

        if (!confirmDeletionDialog.Confirmed) return;

        ActivityViewModels.Remove(SelectedActivityViewModel);

        IsEditing = false;
        IsCreatingNew = false;

        SelectedActivityViewModel = ActivityViewModels.FirstOrDefault();
    }

    [RelayCommand]
    private void DeleteDataEntry(object parameter)
    {
        if (parameter is not DataEntry entry || SelectedActivityViewModel == null) return;

        // Apply deletion immediately to both the actual activity and the edit view collection
        var activity = SelectedActivityViewModel.BaseActivity;

        switch (activity)
        {
            case NumericActivity numActivity when entry is NumericDataEntry numEntry:
                numActivity.Entries.Remove(numEntry);
                break;
            case RoutineActivity routActivity when entry is RoutineDataEntry routEntry:
                routActivity.Entries.Remove(routEntry);
                break;
        }

        EditActivityDataEntries.Remove(entry);
    }

    [RelayCommand]
    private async Task EditDataEntry(object parameter)
    {
        if (parameter is not DataEntry entry || SelectedActivityViewModel == null) return;

        var activity = SelectedActivityViewModel.BaseActivity;

        if (activity is NumericActivity numericActivity && entry is NumericDataEntry numericEntry)
        {
            var dialog = new NumericEntryDialogViewModel
            {
                Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Save"),
                UnitText = numericActivity.Unit,
                SelectedDate = numericEntry.Timestamp,
                NumericValue = numericEntry.Value
            };

            await _dialogService.ShowDialogAsync(dialog);

            if (dialog is { Confirmed: true, NumericValue: not null })
            {
                // Creating a new instance to replace the old one ensures proper UI collection update
                var updatedEntry = new NumericDataEntry
                {
                    Id = numericEntry.Id,
                    Timestamp = dialog.SelectedDate ?? DateTime.Now,
                    Value = dialog.NumericValue.Value
                };

                ReplaceEntry(numericActivity.Entries, numericEntry, updatedEntry);
                ReplaceEntry(EditActivityDataEntries, numericEntry, updatedEntry);
            }
        }
        else if (activity is RoutineActivity routineActivity && entry is RoutineDataEntry routineEntry)
        {
            var dialog = new RoutineEntryDialogViewModel
            {
                Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Save"),
                SelectedDate = routineEntry.Timestamp,
                SelectedItem = routineEntry.State
            };

            await _dialogService.ShowDialogAsync(dialog);

            if (dialog.Confirmed)
            {
                if (dialog.SelectedItem == RoutineState.None)
                {
                    // If user selects 'None', it equals deletion in Routine context
                    routineActivity.Entries.Remove(routineEntry);
                    EditActivityDataEntries.Remove(routineEntry);
                }
                else
                {
                    var updatedEntry = new RoutineDataEntry
                    {
                        Id = routineEntry.Id,
                        Timestamp = dialog.SelectedDate,
                        State = dialog.SelectedItem
                    };

                    ReplaceEntry(routineActivity.Entries, routineEntry, updatedEntry);
                    ReplaceEntry(EditActivityDataEntries, routineEntry, updatedEntry);
                }
            }
        }
    }

    /// <summary>
    /// Helper method to replace an item in an ObservableCollection to trigger UI updates seamlessly
    /// </summary>
    private static void ReplaceEntry<T>(ObservableCollection<T> collection, T oldItem, T newItem)
        where T : DataEntry
    {
        var index = collection.IndexOf(oldItem);
        if (index >= 0)
        {
            collection[index] = newItem;
        }
    }
}