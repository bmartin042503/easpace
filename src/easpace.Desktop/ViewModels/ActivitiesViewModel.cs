// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Activities;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels;

public partial class ActivitiesViewModel : PageViewModel
{
    private readonly IDialogService _dialogService;
    public ObservableCollection<ActivityViewModel> ActivityViewModels { get; } = [];

    [ObservableProperty] private ActivityViewModel? _selectedActivityViewModel;

    [ObservableProperty] private ViewModelBase? _contentViewModel;
    
    [ObservableProperty] private bool _isEditing;

    private ActivityEditorViewModel? _editorViewModel;

    
    public bool HasActivities => ActivityViewModels.Any();

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
        catch (Exception ex)
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

    private void OnActivityEditorCancelled(object? sender, EventArgs e)
    {
        CloseEditor();
    }

    private void OpenEditor(ActivityViewModel? activityViewModel = null)
    {
        _editorViewModel = activityViewModel == null
            ? new ActivityEditorViewModel(_dialogService)
            : new ActivityEditorViewModel(activityViewModel, _dialogService);

        _editorViewModel.Saved += OnActivityEditorSaved;
        _editorViewModel.Cancelled += OnActivityEditorCancelled;

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
        _editorViewModel.Cancelled -= OnActivityEditorCancelled;

        _editorViewModel = null;

        IsEditing = false;
    }

    private void InsertViewModel(ActivityViewModel activityViewModel)
    {
        ActivityViewModels.Insert(0, activityViewModel);
        activityViewModel.DeleteRequested += OnActivityDeleteRequested;
        activityViewModel.EditRequested += OnActivityEditRequested;
        
        SelectedActivityViewModel = activityViewModel;
    }

    private void RemoveViewModel(ActivityViewModel activityViewModel)
    {
        ActivityViewModels.Remove(activityViewModel);
        activityViewModel.DeleteRequested -= OnActivityDeleteRequested;
        activityViewModel.EditRequested -= OnActivityEditRequested;
    }
}