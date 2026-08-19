// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services;
using easpace.Desktop.Validation;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class ActivityEditorViewModel : ValidatorViewModelBase
{
    private readonly IActivityEditorService _editorService;
    private readonly IActivityService _activityService;
    private ActivityViewModel? _activity;

    [ObservableProperty] private string _titleText = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "FormValidation.Name.Required")]
    [MinLength(3, ErrorMessage = "FormValidation.Name.MinLength")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = LocalizationService.GetString("Activities.Input.NewActivityName");

    [ObservableProperty] private string? _unit;

    [ObservableProperty] private bool _isTargetChecked;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [RequiredIf(nameof(SelectedType), ActivityType.Milestone, ErrorMessage = "FormValidation.Target.Required")]
    private double? _target;

    [ObservableProperty] private DateTimeOffset? _targetDate;

    private bool CanSubmit() => !HasErrors;

    public event EventHandler<Activity>? Saved;
    public event EventHandler? Canceled;

    public bool IsCreatingNew { get; }

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
        IActivityEditorService editorService, 
        IActivityService activityService)
    {
        _editorService = editorService;
        _activityService = activityService;
        IsCreatingNew = true;
        SelectedType = ActivityType.Trend;
        TitleText = LocalizationService.GetString("Activities.Input.NewActivityName");
    }

    public ActivityEditorViewModel(
        IActivityEditorService editorService, 
        IActivityService activityService,
        ActivityViewModel activity)
    {
        _editorService = editorService;
        _activityService = activityService;
        _activity = activity;
        IsCreatingNew = false;

        var updateRequest = _editorService.GetUpdateRequest(_activity);
        SetFormDataFromUpdateRequest(updateRequest);

        SelectedType = _activity switch
        {
            TrendActivityViewModel => ActivityType.Trend,
            MilestoneActivityViewModel => ActivityType.Milestone,
            RoutineActivityViewModel => ActivityType.Routine,
            _ => SelectedType
        };

        TitleText = string.Format(LocalizationService.GetString("Activities.Title.Edit"), _activity.Name);
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Save()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        if (IsCreatingNew)
        {
            var savedEntry = _activityService.CreateActivity(GetCreateRequest());
            Saved?.Invoke(this, savedEntry);
        }
        else
        {
            if (_activity == null) return;
            
            var updateRequest = _editorService.GetUpdateRequest(_activity);
            var updatedEntry = _activity.UpdateFrom(updateRequest);
            
            if (updatedEntry == null) return;
            
            Saved?.Invoke(this, updatedEntry);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Canceled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void DeleteDataEntry(object? parameter)
    {
        if (parameter is not DataEntryViewModel dataEntryVm || _activity == null || IsCreatingNew) return;
        
        _activity.DeleteDataEntry(dataEntryVm.Id);
    }

    [RelayCommand]
    private void EditDataEntry(object? parameter)
    {
        if (parameter is not DataEntryViewModel dataEntryVm || _activity == null || IsCreatingNew) return;

        // TODO: show edit dialog and pass DTO to the method below
        
        // _activity.UpdateDataEntry(dataEntryVm.Id, );
    }

    private void SetFormDataFromUpdateRequest(UpdateActivityRequest updateRequest)
    {
        Name = updateRequest.Name;
        Unit = updateRequest.Unit;
        Target = updateRequest.Target;
        TargetDate = updateRequest.TargetDate;
    }

    private CreateActivityRequest GetCreateRequest() => new (Name, SelectedType, Target, Unit, TargetDate);
}