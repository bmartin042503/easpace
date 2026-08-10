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
    public ObservableCollection<DataEntry> EditActivityDataEntries { get; set; } = [];

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

    public bool IsTargetInputVisible =>
        EditSelectedActivityType is ActivityType.Milestone ||
        (EditSelectedActivityType is ActivityType.Trend && IsTargetChecked);

    public bool IsTargetDateVisible => EditSelectedActivityType is ActivityType.Milestone;

    public ActivitiesViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;

        Page = ApplicationPage.Activities;

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
                        return;
                    }
                    
                    var index = routineActivity.Entries.IndexOf(existingEntry);
                
                    routineActivity.Entries[index] = new RoutineDataEntry
                    {
                        Id = existingEntry.Id,
                        Timestamp = date,
                        State = state
                    };
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

        EditActivityDataEntries = new(dataEntries);
    }

    // TODO: validate form, add checks for Unit value (Trend & Milestone)

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

            var activity = SelectedActivityViewModel.BaseActivity;
            activity.Title = EditName;

            switch (activity)
            {
                case TrendActivity trendActivity:
                    trendActivity.Unit = EditUnit;
                    trendActivity.Target = IsTargetChecked ? EditTarget : null;

                    var trendEntriesToRemove = trendActivity.Entries
                        .Where(e => !EditActivityDataEntries.Contains(e)).ToList();

                    foreach (var item in trendEntriesToRemove)
                    {
                        trendActivity.Entries.Remove(item);
                    }

                    break;

                case MilestoneActivity milestoneActivity:
                    if (!EditTarget.HasValue) return;

                    milestoneActivity.Unit = EditUnit;
                    milestoneActivity.Target = EditTarget;

                    if (EditTargetDate != null && EditTargetDate >= DateTime.Now)
                    {
                        milestoneActivity.TargetDate = EditTargetDate;
                    }

                    var milestoneEntriesToRemove = milestoneActivity.Entries
                        .Where(e => !EditActivityDataEntries.Contains(e)).ToList();

                    foreach (var item in milestoneEntriesToRemove)
                    {
                        milestoneActivity.Entries.Remove(item);
                    }

                    break;

                case RoutineActivity routineActivity:
                    var routineEntriesToRemove = routineActivity.Entries
                        .Where(e => !EditActivityDataEntries.Contains(e)).ToList();

                    foreach (var item in routineEntriesToRemove)
                    {
                        routineActivity.Entries.Remove(item);
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
    private void DeleteActivity()
    {
        if (SelectedActivityViewModel == null) return;

        ActivityViewModels.Remove(SelectedActivityViewModel);

        IsEditing = false;
        IsCreatingNew = false;

        SelectedActivityViewModel = ActivityViewModels.FirstOrDefault();
    }

    [RelayCommand]
    private void DeleteDataEntry(DataEntry? entry)
    {
        if (entry == null) return;

        EditActivityDataEntries.Remove(entry);
    }
}