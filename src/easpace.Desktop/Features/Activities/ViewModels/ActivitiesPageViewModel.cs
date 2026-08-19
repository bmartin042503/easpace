// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class ActivitiesPageViewModel : PageViewModel
{
    private readonly IActivityService _activityService;
    private readonly IActivityEditorService _activityEditorService;
    private readonly IDataEntryService _dataEntryService;
    private readonly IDialogService _dialogService;
    private readonly ITrendActivityDataProvider _trendActivityDataProvider;
    private readonly IRoutineActivityDataProvider _routineActivityDataProvider;

    private readonly List<ActivityViewModel> _allActivities = [];

    private ActivityEditorViewModel? _editorViewModel;

    public AvaloniaList<ActivityViewModel> Activities = [];

    [ObservableProperty] private ObservableObject? _contentViewModel;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private ActivityViewModel? _selectedActivity;

    public bool HasActivities => _allActivities.Count > 0;

    public ActivitiesPageViewModel(
        IActivityService activityService,
        IActivityEditorService activityEditorService,
        IDataEntryService dataEntryService,
        IDialogService dialogService,
        ITrendActivityDataProvider trendActivityDataProvider,
        IRoutineActivityDataProvider routineActivityDataProvider)
    {
        Page = ApplicationPage.Activities;

        _activityService = activityService;
        _activityEditorService = activityEditorService;
        _dataEntryService = dataEntryService;
        _dialogService = dialogService;
        _trendActivityDataProvider = trendActivityDataProvider;
        _routineActivityDataProvider = routineActivityDataProvider;

        LoadActivities();
    }

    private void LoadActivities()
    {
        _allActivities.Clear();

        foreach (var activity in _activityService.GetActivities())
        {
            switch (activity)
            {
                case TrendActivity trendActivity:

                    var trendActivityViewModel = new TrendActivityViewModel(
                        trendActivity, _trendActivityDataProvider,
                        _dataEntryService, _activityService);

                    _allActivities.Add(trendActivityViewModel);

                    break;

                case MilestoneActivity milestoneActivity:

                    var milestoneActivityViewModel =
                        new MilestoneActivityViewModel(milestoneActivity, _dataEntryService, _activityService);

                    _allActivities.Add(milestoneActivityViewModel);

                    break;

                case RoutineActivity routineActivity:

                    var routineActivityViewModel =
                        new RoutineActivityViewModel(
                            routineActivity, _routineActivityDataProvider,
                            _dataEntryService, _activityService);

                    _allActivities.Add(routineActivityViewModel);

                    break;
            }
        }
        
        Activities.AddRange(_allActivities);
    }

    [RelayCommand]
    private void AddActivity()
    {
        OpenEditor();
    }

    [RelayCommand]
    private async Task DeleteActivity()
    {
        if (SelectedActivity is null) return;
        
        var confirmDeletionDialog = new ConfirmDialogViewModel
        {
            Title = string.Format(LocalizationService.GetString("Activities.DeleteDialog.Title"),
                SelectedActivity.Name),
            Message = LocalizationService.GetString("Activities.DeleteDialog.Message"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
            IsDestructive = true,
        };
        
        await _dialogService.ShowDialogAsync(confirmDeletionDialog);
        
        if (!confirmDeletionDialog.Confirmed) return;

        var deleted = _activityService.DeleteActivity(SelectedActivity.Id);

        if (!deleted) return;

        _allActivities.Remove(SelectedActivity);
        Activities.Remove(SelectedActivity);
        
        SelectedActivity = Activities.FirstOrDefault();
    }

    [RelayCommand]
    private void EditActivity()
    {
        if (SelectedActivity is null) return;
        OpenEditor(SelectedActivity);
    }

    private void OpenEditor(ActivityViewModel? activity = null)
    {
        if (_editorViewModel is not null) return;

        var editor = activity is null
            ? new ActivityEditorViewModel(_activityEditorService, _activityService)
            : new ActivityEditorViewModel(_activityEditorService, _activityService, activity);

        editor.Saved += OnEditorSaved;
        editor.Canceled += OnEditorCanceled;

        ContentViewModel = editor;
    }

    private void CloseEditor()
    {
        if (SelectedActivity == null && Activities.Any())
        {
            SelectedActivity = Activities.FirstOrDefault();
        }

        ContentViewModel = SelectedActivity;

        if (_editorViewModel == null) return;

        _editorViewModel.Saved -= OnEditorSaved;
        _editorViewModel.Canceled -= OnEditorCanceled;

        _editorViewModel = null;

        IsEditing = false;
    }

    private void OnEditorSaved(object? sender, Activity activity)
    {
        if (sender is ActivityEditorViewModel { IsCreatingNew: true })
        {
            switch (activity)
            {
                case TrendActivity trendActivity:
                    var trendActivityVm = new TrendActivityViewModel(
                        trendActivity,
                        _trendActivityDataProvider,
                        _dataEntryService,
                        _activityService);
                    _allActivities.Add(trendActivityVm);
                    Activities.Insert(0, trendActivityVm);
                    break;

                case MilestoneActivity milestoneActivity:
                    var milestoneActivityVm = new MilestoneActivityViewModel(
                        milestoneActivity,
                        _dataEntryService,
                        _activityService);
                    _allActivities.Add(milestoneActivityVm);
                    Activities.Insert(0, milestoneActivityVm);
                    break;

                case RoutineActivity routineActivity:
                    var routineActivityVm = new RoutineActivityViewModel(
                        routineActivity,
                        _routineActivityDataProvider,
                        _dataEntryService,
                        _activityService);
                    _allActivities.Add(routineActivityVm);
                    Activities.Insert(0, routineActivityVm);
                    break;

                default:
                    throw new NotSupportedException($"Undefined activity type: {activity.GetType().Name}");
            }
        }

        // editor updates the view model by calling UpdateFrom() with an update request

        CloseEditor();
    }

    private void OnEditorCanceled(object? sender, EventArgs e)
    {
        CloseEditor();
    }
}