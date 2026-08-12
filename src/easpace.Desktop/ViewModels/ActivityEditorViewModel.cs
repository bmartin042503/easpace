// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;
using easpace.Desktop.Services;
using easpace.Desktop.Validation;
using easpace.Desktop.ViewModels.Activities;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels;

public partial class ActivityEditorViewModel : ValidatorViewModelBase
{
    #region Fields

    private readonly IDialogService _dialogService;
    private ActivityViewModel? _editActivityViewModel;

    [ObservableProperty] private string _titleText = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "FormValidation.Name.Required")]
    [MinLength(3, ErrorMessage = "FormValidation.Name.MinLength")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = LocalizationService.GetString("Activities.Input.NewActivityName");

    [ObservableProperty] private string _unit = string.Empty;

    [ObservableProperty] private bool _isTargetChecked;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RequiredIf(nameof(SelectedType), ActivityType.Milestone, ErrorMessage = "FormValidation.Target.Required")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double? _target;

    [ObservableProperty] private DateTime? _targetDate;

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the activity is successfully saved.
    /// </summary>
    public event EventHandler<ActivityViewModel>? Saved;

    /// <summary>
    /// Occurs when the editing process is canceled.
    /// </summary>
    public event EventHandler? Canceled;

    #endregion

    #region Properties

    public bool IsCreatingNew { get; }

    public IEnumerable<DataEntry>? DataEntries { get; }

    public IEnumerable<ActivityType> ActivityTypes { get; } = Enum.GetValues<ActivityType>();

    public ActivityType? SelectedType
    {
        get;
        set
        {
            SetProperty(ref field, value);
            ValidateAllProperties();
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private int DataEntriesCount => (DataEntries as System.Collections.ICollection)?.Count ?? 0;

    public string DataEntriesLabel =>
        DataEntriesCount == 1
            ? LocalizationService.GetString("Activities.Label.DataEntry")
            : string.Format(LocalizationService.GetString("Activities.Label.DataEntries"), DataEntriesCount);

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityEditorViewModel"/> class for creating a new activity.
    /// </summary>
    /// <param name="dialogService">The dialog service used for showing UI dialogs.</param>
    public ActivityEditorViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        IsCreatingNew = true;
        SelectedType = ActivityType.Trend;
        TitleText = LocalizationService.GetString("Activities.Input.NewActivityName");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityEditorViewModel"/> class for editing an existing activity.
    /// </summary>
    /// <param name="activityViewModel">The activity view model to be edited.</param>
    /// <param name="dialogService">The dialog service used for showing UI dialogs.</param>
    public ActivityEditorViewModel(ActivityViewModel activityViewModel, IDialogService dialogService)
    {
        _dialogService = dialogService;
        IsCreatingNew = false;
        _editActivityViewModel = activityViewModel;

        var activity = activityViewModel.BaseActivity;
        TitleText = string.Format(LocalizationService.GetString("Activities.Title.Edit"), activity.Name);
        Name = activity.Name;

        // populate specific fields based on the concrete activity type
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
                
                TargetDate = milestoneActivity.HasValidTargetDate 
                    ? milestoneActivity.TargetDate!.Value.Date 
                    : null;
                
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
            };
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Saves the created or edited activity if validation passes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Save()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        ActivityViewModel? vm;

        if (IsCreatingNew)
        {
            vm = CreateNewActivityViewModel();
        }
        else
        {
            // fallback safety check
            if (_editActivityViewModel == null) return;

            UpdateActivityViewModel(_editActivityViewModel);
            vm = _editActivityViewModel;
        }

        Saved?.Invoke(this, vm);
    }

    /// <summary>
    /// Cancels the editing process and fires the cancellation event.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Canceled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Deletes a specific data entry from the activity's collection.
    /// </summary>
    /// <param name="parameter">The data entry object to be deleted.</param>
    [RelayCommand]
    private void DeleteDataEntry(object parameter)
    {
        // verify entry type and ensure we are in edit mode before deleting
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

    /// <summary>
    /// Opens a dialog to edit an existing data entry.
    /// </summary>
    /// <param name="parameter">The data entry object to be edited.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task EditDataEntry(object parameter)
    {
        // abort if the parameter is invalid, or we are not in edit mode
        if (parameter is not DataEntry entry || IsCreatingNew || _editActivityViewModel == null) return;

        switch (entry)
        {
            case NumericDataEntry numericDataEntry:
                if (_editActivityViewModel.BaseActivity is not NumericActivity numericActivity) return;

                // setup numeric entry dialog
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

                // apply changes if the user confirmed
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

                // setup routine entry dialog
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

                // apply changes if the user confirmed
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
        
        // refresh data entries, so the TrendChart can show updated aggregated values
        _editActivityViewModel.RefreshDataEntries();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Determines whether the save command can be executed.
    /// </summary>
    /// <returns>True if there are no validation errors; otherwise, false.</returns>
    private bool CanSubmit() => !HasErrors;

    /// <summary>
    /// Creates a new activity view model based on the selected type and populated fields.
    /// </summary>
    /// <returns>A newly created <see cref="ActivityViewModel"/> instance.</returns>
    private ActivityViewModel CreateNewActivityViewModel()
    {
        ActivityViewModel vm;

        // instantiate the concrete activity class based on current selection
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
                    TargetDate = TargetDate.HasValue && TargetDate.Value > DateTime.MinValue
                        ? new DateTimeOffset(TargetDate.Value) 
                        : null
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

    /// <summary>
    /// Updates the underlying model of an existing activity view model with the current field values.
    /// </summary>
    /// <param name="vm">The activity view model to update.</param>
    private void UpdateActivityViewModel(ActivityViewModel vm)
    {
        // only update name if it contains valid text
        if (!string.IsNullOrWhiteSpace(Name))
        {
            vm.BaseActivity.Name = Name;
        }

        // update specific properties based on the activity type
        switch (vm.BaseActivity)
        {
            case TrendActivity trendActivity:
                trendActivity.Unit = Unit;
                trendActivity.Target = IsTargetChecked ? Target : null;
                break;

            case MilestoneActivity milestoneActivity:
                milestoneActivity.Unit = Unit;
                milestoneActivity.Target = Target;
                milestoneActivity.TargetDate = TargetDate.HasValue && TargetDate.Value > DateTime.MinValue
                    ? new DateTimeOffset(TargetDate.Value) 
                    : null;
                break;
        }
    }

    #endregion
}