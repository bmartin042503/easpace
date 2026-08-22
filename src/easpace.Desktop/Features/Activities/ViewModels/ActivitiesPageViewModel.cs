// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Activities.Entities;
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

    public AvaloniaList<ActivityViewModel> Activities { get; } = [];

    [ObservableProperty] private ObservableObject? _contentViewModel;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private ActivityViewModel? _selectedActivity;

    public bool HasActivities => _allActivities.Count > 0;

    public int SelectedFilterIndex
    {
        get;
        set
        {
            SetProperty(ref field, value);
            FilterActivities();
        }
    }

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
            InsertViewModel(activity);
        }

        Activities.AddRange(_allActivities);
    }

    partial void OnSelectedActivityChanged(ActivityViewModel? value)
    {
        if (_editorViewModel == null)
        {
            ContentViewModel = value;
        }
    }

    [RelayCommand]
    private void AddActivity()
    {
        OpenEditor();
    }

    private void OpenEditor(ActivityViewModel? activity = null)
    {
        if (_editorViewModel is not null) return;

        var editor = activity is null
            ? new ActivityEditorViewModel(_activityService)
            : new ActivityEditorViewModel(_activityEditorService, _activityService, activity);

        editor.Saved += OnEditorSaved;
        editor.Canceled += OnEditorCanceled;

        ContentViewModel = editor;
        
        IsEditing = true;
    }

    private void CloseEditor()
    {
        if (SelectedActivity == null && Activities.Count > 0)
        {
            SelectedActivity = Activities.FirstOrDefault();
        }

        ContentViewModel = SelectedActivity;
        
        IsEditing = false;

        if (_editorViewModel == null) return;

        _editorViewModel.Saved -= OnEditorSaved;
        _editorViewModel.Canceled -= OnEditorCanceled;

        _editorViewModel = null;
    }

    private void OnEditorSaved(object? sender, Activity activity)
    {
        if (sender is ActivityEditorViewModel { IsCreatingNew: true })
        {
            InsertViewModel(activity);
        }

        // editor updates the view model by calling UpdateFrom() with an update request

        CloseEditor();
    }

    private void InsertViewModel(Activity activity)
    {
        ActivityViewModel? activityViewModel = null;
        
        switch (activity)
        {
            case TrendActivity trendActivity:
                activityViewModel = new TrendActivityViewModel(
                    trendActivity, _trendActivityDataProvider, _dataEntryService, _activityService, _dialogService
                );
                break;
            
            case MilestoneActivity milestoneActivity:
                activityViewModel = new MilestoneActivityViewModel(
                    milestoneActivity, _dataEntryService, _activityService, _dialogService
                );
                break;
            
            case RoutineActivity routineActivity:
                activityViewModel = new RoutineActivityViewModel(
                    routineActivity, _routineActivityDataProvider, _dataEntryService, _activityService, _dialogService
                );
                break;
        }

        if (activityViewModel is null) return;
        _allActivities.Add(activityViewModel);
        Activities.Insert(0, activityViewModel);
        SubscribeToActivityEvents(activityViewModel);
        
        OnPropertyChanged(nameof(HasActivities));
        
        FilterActivities();

        if (Activities.Contains(activityViewModel))
        {
            SelectedActivity = activityViewModel;
        }
    }

    private void OnEditorCanceled(object? sender, EventArgs e)
    {
        CloseEditor();
    }

    private void OnActivityEditRequested(object? sender, EventArgs e)
    {
        if (SelectedActivity is null) return;
        OpenEditor(SelectedActivity);
    }

    private async void OnActivityDeleteRequested(object? sender, EventArgs e)
    {
        if (sender is not ActivityViewModel activityViewModel) return;

        try
        {
            var confirmDeletionDialog = new ConfirmDialogViewModel
            {
                Title = string.Format(LocalizationService.GetString("Activities.DeleteDialog.Title"),
                    activityViewModel.Name),
                Message = LocalizationService.GetString("Activities.DeleteDialog.Message"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
                IsDestructive = true,
            };

            await _dialogService.ShowDialogAsync(confirmDeletionDialog);

            if (!confirmDeletionDialog.Confirmed) return;

            var deleted = _activityService.DeleteActivity(activityViewModel.Id);

            if (!deleted) return;

            UnsubscribeFromActivityEvents(activityViewModel);

            _allActivities.Remove(activityViewModel);
            Activities.Remove(activityViewModel);
            
            OnPropertyChanged(nameof(HasActivities));

            SelectedActivity = Activities.FirstOrDefault();
        }
        catch (Exception)
        {
            // TODO: proper logging
        }
    }

    private void FilterActivities()
    {
        var showArchivedActivities = SelectedFilterIndex == 1;

        var filteredItems = _allActivities
            .Where(vm => vm.IsArchived == showArchivedActivities)
            .ToList();

        Activities.Clear();
        Activities.AddRange(filteredItems);

        SelectedActivity = Activities.FirstOrDefault();
    }

    private void OnActivityArchiveToggled(object? sender, EventArgs e)
    {
        FilterActivities();
    }

    private void SubscribeToActivityEvents(ActivityViewModel activityViewModel)
    {
        activityViewModel.EditRequested += OnActivityEditRequested;
        activityViewModel.DeleteRequested += OnActivityDeleteRequested;
        activityViewModel.ArchiveToggled += OnActivityArchiveToggled;
    }

    private void UnsubscribeFromActivityEvents(ActivityViewModel activityViewModel)
    {
        activityViewModel.EditRequested += OnActivityEditRequested;
        activityViewModel.DeleteRequested += OnActivityDeleteRequested;
        activityViewModel.ArchiveToggled += OnActivityArchiveToggled;
    }
}