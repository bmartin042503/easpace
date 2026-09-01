// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ValidationAttributes;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal partial class ActivityEditorViewModel : ValidatorViewModelBase
{
    private readonly IActivityService _activityService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ActivityEditorViewModel> _logger;
    private ActivityViewModel? _activity;

    [ObservableProperty] private string _titleText = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "FormValidation.Name.Required")]
    [MinLength(3, ErrorMessage = "FormValidation.Name.MinLength")]
    [MaxLength(64, ErrorMessage = "FormValidation.Name.MaxLength")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = LocalizationService.GetString("Activities.Input.NewActivityName");

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(16, ErrorMessage = "FormValidation.Unit.MaxLength")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _unit; 

    [ObservableProperty] private bool _isTargetChecked;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0d, 10_000_000d, MinimumIsExclusive = true, ErrorMessage = "FormValidation.Target.Range")]
    [RequiredIf(nameof(SelectedType), ActivityType.Milestone, ErrorMessage = "FormValidation.Target.Required")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double? _target;
    
    [ObservableProperty] private DateTime? _targetDate;
    [ObservableProperty] private DateTime? _startDate;

    private bool CanSubmit() => !HasErrors;

    public event EventHandler<Activity>? Saved;
    public event EventHandler? Canceled;

    public AvaloniaList<ActivityDataEntryViewModel> DataEntries { get; init; }
    
    public IEnumerable<ActivityType> ActivityTypes { get; } = Enum.GetValues<ActivityType>();

    public bool IsCreatingNew { get; }
    
    public string DataEntriesLabel =>
        DataEntries.Count == 1
            ? LocalizationService.GetString("Activities.Label.DataEntry")
            : string.Format(LocalizationService.GetString("Activities.Label.DataEntries"), DataEntries.Count);

    public ActivityType SelectedType
    {
        get;
        set
        {
            SetProperty(ref field, value);
            ValidateAllProperties();
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    public ActivityEditorViewModel(
        IActivityService activityService, 
        IDialogService dialogService, 
        ILogger<ActivityEditorViewModel> logger)
    {
        _activityService = activityService;
        _dialogService = dialogService;
        _logger = logger;
        IsCreatingNew = true;
        SelectedType = ActivityType.Trend;
        TitleText = LocalizationService.GetString("Activities.Input.NewActivityName");
        
        DataEntries = [];
        
        ValidateAllProperties();
        SaveCommand.NotifyCanExecuteChanged();
    }

    public ActivityEditorViewModel(
        IActivityEditorService editorService, 
        IActivityService activityService,
        IDialogService dialogService,
        ActivityViewModel activity,
        ILogger<ActivityEditorViewModel> logger)
    {
        _activityService = activityService;
        _dialogService = dialogService;
        _logger = logger;
        _activity = activity;
        IsCreatingNew = false;

        var updateRequest = editorService.GetUpdateRequest(_activity);
        SetFormDataFromUpdateRequest(updateRequest);

        SelectedType = _activity switch
        {
            TrendActivityViewModel => ActivityType.Trend,
            MilestoneActivityViewModel => ActivityType.Milestone,
            RoutineActivityViewModel => ActivityType.Routine,
            _ => SelectedType
        };

        DataEntries = _activity.Entries;
        
        DataEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DataEntriesLabel));

        TitleText = string.Format(LocalizationService.GetString("Activities.Title.Edit"), _activity.Name);
        
        ValidateAllProperties();
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsTargetCheckedChanged(bool value)
    {
        if (!value)
        {
            // we make sure to set target value to null if the target option is not checked
            Target = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task Save()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        try
        {
            if (IsCreatingNew)
            {
                _logger.LogInformation("Saving new activity from editor");
                var savedEntry = await _activityService.CreateActivityAsync(GetCreateRequest());
                Saved?.Invoke(this, savedEntry);
            }
            else
            {
                if (_activity == null) return;
            
                _logger.LogInformation("Updating existing activity with ID {Id} from editor", _activity.Id);
                var updateRequest = GetUpdateRequest();
                var updatedEntry = await _activity.UpdateFrom(updateRequest);
            
                if (updatedEntry == null) return;
            
                Saved?.Invoke(this, updatedEntry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save activity from editor (IsCreatingNew: {IsCreatingNew})", IsCreatingNew);
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Activities.Error.SaveFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Canceled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task DeleteDataEntry(object? parameter)
    {
        if (parameter is not ActivityDataEntryViewModel dataEntryVm || _activity == null || IsCreatingNew) return;

        try
        {
            var deleted = await _activity.DeleteDataEntryAsync(dataEntryVm.Id);

            if (deleted)
            {
                _logger.LogInformation("Data entry {EntryId} removed successfully via editor", dataEntryVm.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete data entry {EntryId} via editor", dataEntryVm.Id);
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Activities.Error.EntryDeleteFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    [RelayCommand]
    private async Task EditDataEntry(object? parameter)
    {
        if (parameter is not ActivityDataEntryViewModel dataEntryVm || _activity == null || IsCreatingNew) return;

        await _activity.EditDataEntry(dataEntryVm.Id);
    }

    private void SetFormDataFromUpdateRequest(UpdateActivityRequest updateRequest)
    {
        Name = updateRequest.Name;
        Unit = updateRequest.Unit;
        Target = updateRequest.Target;
        IsTargetChecked = updateRequest.Target.HasValue;
        StartDate = ToDateTime(updateRequest.StartDate);
        TargetDate = ToDateTime(updateRequest.TargetDate);
    }

    private CreateActivityRequest GetCreateRequest()
    {
        return new CreateActivityRequest(
            Name: Name,
            Type: SelectedType,

            Target: SelectedType switch
            {
                ActivityType.Milestone => Target,
                ActivityType.Trend when IsTargetChecked => Target,
                _ => null
            },

            Unit: SelectedType is ActivityType.Trend or ActivityType.Milestone
                ? Unit
                : null,

            StartDate: SelectedType == ActivityType.Milestone
                ? ToDateOnly(StartDate)
                : null,

            TargetDate: SelectedType == ActivityType.Milestone
                ? ToDateOnly(TargetDate)
                : null
        );
    }

    private UpdateActivityRequest GetUpdateRequest()
    {
        if (_activity == null)
        {
            throw new InvalidOperationException(
                "Cannot create an update request without an activity.");
        }

        return new UpdateActivityRequest(
            Name: Name,
            Target: Target,
            Unit: Unit,
            StartDate: ToDateOnly(StartDate),
            TargetDate: ToDateOnly(TargetDate)
        );
    }

    private static DateOnly? ToDateOnly(DateTime? dateTime)
    {
        if (!dateTime.HasValue || dateTime.Value == DateTime.MinValue)
        {
            return null;
        }

        return DateOnly.FromDateTime(dateTime.Value);
    }

    private static DateTime? ToDateTime(DateOnly? dateOnly)
    {
        if (!dateOnly.HasValue || dateOnly.Value == DateOnly.MinValue)
        {
            return null;
        }

        return dateOnly.Value.ToDateTime(TimeOnly.MinValue);
    }
}