// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Activities;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels;

public partial class ActivityEditorViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    public IEnumerable<DataEntry>? DataEntries { get; private set; }

    [ObservableProperty] private ActivityType? _selectedType;

    [ObservableProperty] private string _titleText = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _unit = string.Empty;

    [ObservableProperty] private bool _isTargetChecked;

    [ObservableProperty] private double? _target;
    [ObservableProperty] private DateTimeOffset? _targetDate;

    private ActivityViewModel? _editActivityViewModel;
    public bool IsCreatingNew { get; }

    public IEnumerable<ActivityType> ActivityTypes { get; } = Enum.GetValues<ActivityType>();

    public bool HasDataEntries => DataEntries is not null && DataEntriesCount > 0;
    private int DataEntriesCount => (DataEntries as System.Collections.ICollection)?.Count ?? 0;
    public string DataEntriesLabel =>
        DataEntriesCount == 1
            ? LocalizationService.GetString("Activities.Label.DataEntry")
            : string.Format(LocalizationService.GetString("Activities.Label.DataEntries"),
                DataEntriesCount);

    public event EventHandler<ActivityViewModel>? Saved;
    public event EventHandler? Cancelled;

    public ActivityEditorViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        IsCreatingNew = true;
        SelectedType = ActivityType.Trend;
        TitleText = LocalizationService.GetString("Activities.Input.NewActivityName");
    }

    public ActivityEditorViewModel(ActivityViewModel activityViewModel, IDialogService dialogService)
    {
        _dialogService = dialogService;
        IsCreatingNew = false;
        _editActivityViewModel = activityViewModel;

        var activity = activityViewModel.BaseActivity;
        TitleText = string.Format(LocalizationService.GetString("Activities.Title.Edit"), activity.Name);

        Name = activity.Name;

        switch (activity)
        {
            case TrendActivity trendActivity:
                SelectedType = ActivityType.Trend;
                Unit = trendActivity.Unit ?? string.Empty;
                IsTargetChecked = trendActivity.Target.HasValue;
                Target = trendActivity.Target;

                DataEntries = trendActivity.Entries;
                break;

            case MilestoneActivity milestoneActivity:
                SelectedType = ActivityType.Milestone;
                Unit = milestoneActivity.Unit ?? string.Empty;
                Target = milestoneActivity.Target;
                TargetDate = milestoneActivity.TargetDate;

                DataEntries = milestoneActivity.Entries;
                break;

            case RoutineActivity routineActivity:
                SelectedType = ActivityType.Routine;

                DataEntries = routineActivity.Entries;
                break;
        }

        if (DataEntries is INotifyCollectionChanged observableCollection)
        {
            observableCollection.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(DataEntriesLabel));
                OnPropertyChanged(nameof(HasDataEntries));
            };
        }
    }

    [RelayCommand]
    private void Save()
    {
        ActivityViewModel? vm;

        if (IsCreatingNew)
        {
            vm = CreateNewActivityViewModel();
        }
        else
        {
            if (_editActivityViewModel == null) return;
            UpdateActivityViewModel(_editActivityViewModel);
            vm = _editActivityViewModel;
        }

        Saved?.Invoke(this, vm);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void DeleteDataEntry(object parameter)
    {
        if (parameter is not DataEntry entry || IsCreatingNew || _editActivityViewModel == null) return;

        switch (entry)
        {
            case NumericDataEntry numericDataEntry:
                if (_editActivityViewModel.BaseActivity is not NumericActivity numericActivity) return;
                numericActivity.Entries.Remove(numericDataEntry);
                break;

            case RoutineDataEntry routineDataEntry:
                if (_editActivityViewModel.BaseActivity is not RoutineActivity routineActivity) return;
                routineActivity.Entries.Remove(routineDataEntry);
                break;
        }
    }

    [RelayCommand]
    private async Task EditDataEntry(object parameter)
    {
        if (parameter is not DataEntry entry || IsCreatingNew || _editActivityViewModel == null) return;

        switch (entry)
        {
            case NumericDataEntry numericDataEntry:
                if (_editActivityViewModel.BaseActivity is not NumericActivity numericActivity) return;

                var numericDialog = new NumericEntryDialogViewModel
                {
                    Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
                    ConfirmText = LocalizationService.GetString("Common.Button.Save"),
                    CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                    UnitText = numericActivity.Unit,
                    SelectedDate = numericDataEntry.Timestamp.Date,
                    SelectedTime = numericDataEntry.Timestamp.TimeOfDay,
                    NumericValue = numericDataEntry.Value
                };

                await _dialogService.ShowDialogAsync(numericDialog);

                if (numericDialog.Confirmed)
                {
                    if (numericDialog is { SelectedDate: not null, SelectedTime: not null })
                    {
                        numericDataEntry.Timestamp = numericDialog.GetTimestamp();
                    }

                    if (numericDialog.NumericValue.HasValue)
                    {
                        numericDataEntry.Value = numericDialog.NumericValue.Value;
                    }
                }

                break;

            case RoutineDataEntry routineDataEntry:

                if (_editActivityViewModel.BaseActivity is not RoutineActivity) return;

                var routineDialog = new RoutineEntryDialogViewModel
                {
                    Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
                    ConfirmText = LocalizationService.GetString("Common.Button.Save"),
                    CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                    SelectedDate = routineDataEntry.Timestamp.Date,
                    SelectedTime = routineDataEntry.Timestamp.TimeOfDay,
                    SelectedState = routineDataEntry.State
                };

                await _dialogService.ShowDialogAsync(routineDialog);

                if (routineDialog.Confirmed)
                {
                    if (routineDialog is { SelectedDate: not null, SelectedTime: not null })
                    {
                        routineDataEntry.Timestamp = routineDialog.GetTimestamp();
                    }

                    if (routineDialog.SelectedState != RoutineState.None)
                    {
                        routineDataEntry.State = routineDialog.SelectedState;
                    }
                }

                break;
        }
    }

    private ActivityViewModel CreateNewActivityViewModel()
    {
        ActivityViewModel vm;

        switch (SelectedType)
        {
            case ActivityType.Trend:
                var trendActivity = new TrendActivity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.Now,
                    Name = Name,
                    Unit = Unit,
                    Target = Target
                };
                vm = new TrendActivityViewModel(trendActivity, _dialogService);
                break;

            case ActivityType.Milestone:
                var milestoneActivity = new MilestoneActivity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.Now,
                    Name = Name,
                    Unit = Unit,
                    Target = Target,
                    TargetDate = TargetDate
                };
                vm = new MilestoneActivityViewModel(milestoneActivity, _dialogService);
                break;

            case ActivityType.Routine:
                var routineActivity = new RoutineActivity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.Now,
                    Name = Name
                };
                vm = new RoutineActivityViewModel(routineActivity, _dialogService);
                break;

            default:
                throw new InvalidOperationException("Unknown activity type");
        }

        return vm;
    }

    private void UpdateActivityViewModel(ActivityViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(Name)) vm.BaseActivity.Name = Name;

        switch (vm.BaseActivity)
        {
            case TrendActivity trendActivity:
                trendActivity.Unit = Unit;
                trendActivity.Target = IsTargetChecked ? Target : null;
                break;

            case MilestoneActivity milestoneActivity:
                milestoneActivity.Unit = Unit;
                milestoneActivity.Target = Target;
                milestoneActivity.TargetDate = TargetDate;
                break;
        }
    }
}