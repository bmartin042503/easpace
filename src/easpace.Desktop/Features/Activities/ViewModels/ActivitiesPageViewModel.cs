// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal partial class ActivitiesPageViewModel : PageViewModel
{
    private readonly IActivityService _activityService;
    private readonly IActivityEditorService _activityEditorService;
    private readonly IActivityDataEntryService _activityDataEntryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ActivitiesPageViewModel> _logger;
    private readonly ILogger<ActivityEditorViewModel> _editorLogger;
    private readonly ITrendActivityDataProvider _trendActivityDataProvider;
    private readonly IRoutineActivityDataProvider _routineActivityDataProvider;

    private readonly List<ActivityViewModel> _allActivities = [];

    private ActivityEditorViewModel? _editorViewModel;

    public AvaloniaList<ActivityViewModel> Activities { get; } = [];

    [ObservableProperty] private ObservableObject? _contentViewModel;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private ActivityViewModel? _selectedActivity;

    public bool HasActivities => _allActivities.Count > 0;

    private bool _isInitialized;

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
        IActivityDataEntryService activityDataEntryService,
        IDialogService dialogService,
        ILogger<ActivitiesPageViewModel> logger,
        ILogger<ActivityEditorViewModel> editorLogger,
        ITrendActivityDataProvider trendActivityDataProvider,
        IRoutineActivityDataProvider routineActivityDataProvider)
    {
        Page = ApplicationPage.Activities;

        _activityService = activityService;
        _activityEditorService = activityEditorService;
        _activityDataEntryService = activityDataEntryService;
        _dialogService = dialogService;
        _logger = logger;
        _editorLogger = editorLogger;
        _trendActivityDataProvider = trendActivityDataProvider;
        _routineActivityDataProvider = routineActivityDataProvider;
    }

    private async Task LoadActivities()
    {
        var activities = await _activityService.GetActivitiesAsync();

        var activityViewModels = activities
            .Select(CreateViewModel)
            .ToList();

        activityViewModels.ForEach(SubscribeToActivityEvents);

        _allActivities.AddRange(activityViewModels);
        Activities.AddRange(activityViewModels);

        FilterActivities();

        SelectedActivity = activityViewModels.FirstOrDefault();

        OnPropertyChanged(nameof(HasActivities));
    }

    partial void OnSelectedActivityChanged(ActivityViewModel? value)
    {
        if (_editorViewModel == null)
        {
            ContentViewModel = value;
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            await LoadActivities();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize activities page and load activities");

            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Activities.Error.LoadFailed")
            };

            await _dialogService.ShowDialogAsync(errorDialog);
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
            ? new ActivityEditorViewModel(_activityService, _dialogService, _editorLogger)
            : new ActivityEditorViewModel(_activityEditorService, _activityService, _dialogService, activity,
                _editorLogger);

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
            var vm = CreateViewModel(activity);

            _allActivities.Add(vm);
            Activities.Insert(0, vm);
            SubscribeToActivityEvents(vm);

            OnPropertyChanged(nameof(HasActivities));

            FilterActivities();

            if (Activities.Contains(vm))
            {
                SelectedActivity = vm;
            }
        }

        // editor updates the view model by calling UpdateFrom() with an update request

        CloseEditor();
    }

    private ActivityViewModel CreateViewModel(Activity activity)
    {
        switch (activity)
        {
            case TrendActivity trendActivity:
                return new TrendActivityViewModel(
                    trendActivity, _trendActivityDataProvider, _activityDataEntryService, _activityService,
                    _dialogService
                );

            case MilestoneActivity milestoneActivity:
                return new MilestoneActivityViewModel(
                    milestoneActivity, _activityDataEntryService, _activityService, _dialogService
                );

            case RoutineActivity routineActivity:
                return new RoutineActivityViewModel(
                    routineActivity, _routineActivityDataProvider, _activityDataEntryService, _activityService,
                    _dialogService
                );

            default:
                throw new NotSupportedException("Undefined activity type");
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

            var deleted = await _activityService.DeleteActivityAsync(activityViewModel.Id);

            if (!deleted) return;

            UnsubscribeFromActivityEvents(activityViewModel);

            _allActivities.Remove(activityViewModel);
            Activities.Remove(activityViewModel);

            OnPropertyChanged(nameof(HasActivities));

            SelectedActivity = Activities.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while attempting to delete activity {ActivityId}",
                activityViewModel.Id);
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Activities.Error.DeleteFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    private void FilterActivities()
    {
        var showArchivedActivities = SelectedFilterIndex == 1;

        var filteredItems = _allActivities
            .Where(vm => vm.IsArchived == showArchivedActivities)
            .OrderByDescending(vm => vm.CreatedAt)
            .ToList();

        Activities.Clear();
        Activities.AddRange(filteredItems);

        SelectedActivity = Activities.FirstOrDefault();
    }

    private async void OnActivityArchiveToggled(object? sender, EventArgs e)
    {
        if (sender is not ActivityViewModel activityViewModel) return;

        try
        {
            await _activityService.ToggleArchiveAsync(activityViewModel.Id);
            activityViewModel.IsArchived = !activityViewModel.IsArchived;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while toggling archive status for activity {ActivityId}",
                activityViewModel.Id);
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Activities.Error.ArchiveFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }

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