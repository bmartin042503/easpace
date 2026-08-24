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
using easpace.Desktop.Services;
using easpace.Desktop.ValidationAttributes;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class ActivityEditorViewModel : ValidatorViewModelBase
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
                DataEntries.Remove(dataEntryVm);
                _activity.Entries.Remove(dataEntryVm);
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
        
        OnPropertyChanged(nameof(MilestoneActivityViewModel.EntriesSum));
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