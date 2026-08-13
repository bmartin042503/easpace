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
    private readonly List<ActivityViewModel> _allActivityViewModels = [];
    public ObservableCollection<ActivityViewModel> ActivityViewModels { get; } = [];

    [ObservableProperty] private ActivityViewModel? _selectedActivityViewModel;

    [ObservableProperty] private ObservableObject? _contentViewModel;

    [ObservableProperty] private bool _isEditing;

    private ActivityEditorViewModel? _editorViewModel;

    public int SelectedFilterIndex
    {
        get;
        set
        {
            SetProperty(ref field, value);
            FilterActivities();
        }
    }


    public bool HasActivities => _allActivityViewModels.Any();

    public ActivitiesViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;

        Page = ApplicationPage.Activities;

        ActivityViewModels.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasActivities));
    }

    partial void OnSelectedActivityViewModelChanged(ActivityViewModel? value)
    {
        if (_editorViewModel == null)
        {
            ContentViewModel = value;
        }
    }

    [RelayCommand]
    private void AddActivity()
    {
        if (_editorViewModel != null) CloseEditor();

        OpenEditor();
    }

    private void EditActivity(ActivityViewModel activityViewModel)
    {
        if (_editorViewModel != null) CloseEditor();

        OpenEditor(activityViewModel);
    }

    private async Task DeleteActivity(ActivityViewModel activityViewModel)
    {
        var confirmDeletionDialog = new ConfirmDialogViewModel
        {
            Title = string.Format(LocalizationService.GetString("Activities.DeleteDialog.Title"),
                activityViewModel.BaseActivity.Name),
            Message = LocalizationService.GetString("Activities.DeleteDialog.Message"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
            IsDestructive = true,
        };

        await _dialogService.ShowDialogAsync(confirmDeletionDialog);

        if (!confirmDeletionDialog.Confirmed) return;

        RemoveViewModel(activityViewModel);

        SelectedActivityViewModel = ActivityViewModels.FirstOrDefault();
    }

    private async void OnActivityDeleteRequested(object? sender, EventArgs e)
    {
        if (sender is not ActivityViewModel activityViewModel) return;

        try
        {
            await DeleteActivity(activityViewModel);
        }
        catch (Exception)
        {
            // TODO: proper logging
        }
    }

    private void OnActivityEditRequested(object? sender, EventArgs e)
    {
        if (sender is not ActivityViewModel activityViewModel) return;
        EditActivity(activityViewModel);
    }

    private void OnActivityEditorSaved(object? sender, ActivityViewModel activityViewModel)
    {
        if (sender is ActivityEditorViewModel { IsCreatingNew: true })
        {
            InsertViewModel(activityViewModel);
        }

        CloseEditor();
    }

    private void OnActivityEditorCanceled(object? sender, EventArgs e)
    {
        CloseEditor();
    }

    private void OpenEditor(ActivityViewModel? activityViewModel = null)
    {
        _editorViewModel = activityViewModel == null
            ? new ActivityEditorViewModel(_dialogService)
            : new ActivityEditorViewModel(activityViewModel, _dialogService);

        _editorViewModel.Saved += OnActivityEditorSaved;
        _editorViewModel.Canceled += OnActivityEditorCanceled;

        ContentViewModel = _editorViewModel;

        IsEditing = true;
    }

    private void CloseEditor()
    {
        if (SelectedActivityViewModel == null && ActivityViewModels.Any())
        {
            SelectedActivityViewModel = ActivityViewModels.FirstOrDefault();
        }

        ContentViewModel = SelectedActivityViewModel;

        if (_editorViewModel == null) return;

        _editorViewModel.Saved -= OnActivityEditorSaved;
        _editorViewModel.Canceled -= OnActivityEditorCanceled;

        _editorViewModel = null;

        IsEditing = false;
    }

    private void InsertViewModel(ActivityViewModel activityViewModel)
    {
        _allActivityViewModels.Insert(0, activityViewModel);
        activityViewModel.DeleteRequested += OnActivityDeleteRequested;
        activityViewModel.EditRequested += OnActivityEditRequested;

        activityViewModel.BaseActivity.PropertyChanged += OnActivityPropertyChanged;

        FilterActivities();

        if (ActivityViewModels.Contains(activityViewModel))
        {
            SelectedActivityViewModel = activityViewModel;
        }
    }

    private void RemoveViewModel(ActivityViewModel activityViewModel)
    {
        _allActivityViewModels.Remove(activityViewModel);
        ActivityViewModels.Remove(activityViewModel);

        activityViewModel.DeleteRequested -= OnActivityDeleteRequested;
        activityViewModel.EditRequested -= OnActivityEditRequested;
        activityViewModel.BaseActivity.PropertyChanged -= OnActivityPropertyChanged;

        if (SelectedActivityViewModel == activityViewModel)
        {
            SelectedActivityViewModel = ActivityViewModels.FirstOrDefault();
        }
    }

    private void FilterActivities()
    {
        var showArchived = SelectedFilterIndex == 1;

        var filteredItems = _allActivityViewModels
            .Where(vm => vm.BaseActivity.IsArchived == showArchived)
            .ToList();

        ActivityViewModels.Clear();
        foreach (var item in filteredItems)
        {
            ActivityViewModels.Add(item);
        }

        SelectedActivityViewModel = ActivityViewModels.FirstOrDefault();
    }

    private void OnActivityPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Activity.IsArchived))
        {
            FilterActivities();
        }
    }
}