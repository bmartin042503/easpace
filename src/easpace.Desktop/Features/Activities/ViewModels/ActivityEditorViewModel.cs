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
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services;
using easpace.Desktop.ValidationAttributes;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class ActivityEditorViewModel : ValidatorViewModelBase
{
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
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double? _target;

    
    [ObservableProperty] 
    [NotifyDataErrorInfo]
    [SafeDateRange(ErrorMessage = "FormValidation.TargetDate.Invalid")]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DateTime? _targetDate;

    private bool CanSubmit() => !HasErrors;

    public event EventHandler<Activity>? Saved;
    public event EventHandler? Canceled;

    public AvaloniaList<DataEntryViewModel> DataEntries { get; } = [];
    
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

    public ActivityEditorViewModel(IActivityService activityService)
    {
        _activityService = activityService;
        IsCreatingNew = true;
        SelectedType = ActivityType.Trend;
        TitleText = LocalizationService.GetString("Activities.Input.NewActivityName");
        
        ValidateAllProperties();
        SaveCommand.NotifyCanExecuteChanged();
    }

    public ActivityEditorViewModel(
        IActivityEditorService editorService, 
        IActivityService activityService,
        ActivityViewModel activity)
    {
        _activityService = activityService;
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
        
        DataEntries.AddRange(activity.Entries);
        
        DataEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DataEntriesLabel));

        TitleText = string.Format(LocalizationService.GetString("Activities.Title.Edit"), _activity.Name);
        
        ValidateAllProperties();
        SaveCommand.NotifyCanExecuteChanged();
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
            
            var updateRequest = GetUpdateRequest();
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
        
        var deleted = _activity.DeleteDataEntry(dataEntryVm.Id);

        if (deleted)
        {
            DataEntries.Remove(dataEntryVm);
        }
    }

    [RelayCommand]
    private async Task EditDataEntry(object? parameter)
    {
        if (parameter is not DataEntryViewModel dataEntryVm || _activity == null || IsCreatingNew) return;

        await _activity.EditDataEntry(dataEntryVm.Id);
    }

    private void SetFormDataFromUpdateRequest(UpdateActivityRequest updateRequest)
    {
        Name = updateRequest.Name;
        Unit = updateRequest.Unit;
        Target = updateRequest.Target;
        IsTargetChecked = updateRequest.Target.HasValue;
        TargetDate = updateRequest.TargetDate?.Date;
    }

    private CreateActivityRequest GetCreateRequest() => new (Name, SelectedType, Target, Unit, TargetDate);
    private UpdateActivityRequest GetUpdateRequest() => new(Name, Target, Unit, TargetDate);
}