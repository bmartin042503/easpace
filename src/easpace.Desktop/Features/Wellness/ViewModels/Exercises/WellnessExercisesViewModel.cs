// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Wellness.Repositories;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.ViewModels.Exercises;

internal partial class WellnessExercisesViewModel : ViewModelBase, IDisposable
{
    private readonly IWellnessExerciseRepository _wellnessExerciseRepository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessExercisesViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Editor))]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    [NotifyPropertyChangedFor(nameof(CanSelectExercise))]
    [NotifyCanExecuteChangedFor(nameof(EditExerciseCommand))]
    private ObservableObject? _contentViewModel;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(EditExerciseCommand))]
    private WellnessExerciseViewModel? _selectedExerciseViewModel;

    [ObservableProperty] private bool _isLoading = true;
    private bool _isInitializationRunning;
    private bool _isDisposed;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    private bool _isInitialized;

    [ObservableProperty] private string _title;

    public WellnessExercisePreviewViewModel PreviewViewModel { get; }

    public AvaloniaList<WellnessExerciseViewModel> WellnessExercises { get; set; } = [];

    public event EventHandler? ConfigurationViewRequested;

    public bool HasExercises => WellnessExercises.Count > 0;
    public bool ShowNoExercises => IsInitialized && !HasExercises;
    public WellnessExerciseEditorViewModel? Editor => ContentViewModel as WellnessExerciseEditorViewModel;
    public bool IsEditing => Editor != null;

    // the selection stays fixed while editing, so the edited exercise is the selected one
    public bool CanSelectExercise => HasExercises && !IsEditing;

    private bool CanEditExercise() => SelectedExerciseViewModel != null && !IsEditing;

    public WellnessExercisesViewModel(
        IWellnessExerciseRepository wellnessExerciseRepository,
        IDialogService dialogService,
        WellnessExercisePreviewViewModel previewViewModel,
        ILogger<WellnessExercisesViewModel> logger)
    {
        _wellnessExerciseRepository = wellnessExerciseRepository;
        _dialogService = dialogService;
        _logger = logger;

        PreviewViewModel = previewViewModel;

        Title = LocalizationService.GetString("Wellness.Button.Exercises");

        WellnessExercises.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasExercises));
            OnPropertyChanged(nameof(ShowNoExercises));
            OnPropertyChanged(nameof(CanSelectExercise));
        };

        ContentViewModel = SelectedExerciseViewModel;
    }

    partial void OnSelectedExerciseViewModelChanged(WellnessExerciseViewModel? value)
    {
        // loading may finish after the user has already navigated away
        if (_isDisposed) return;

        ShowExercise(value);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (IsInitialized || _isInitializationRunning)
        {
            return;
        }

        _isInitializationRunning = true;
        IsLoading = true;

        try
        {
            await LoadWellnessExercises();

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load and initialize wellness exercise data");

            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Wellness.Error.LoadFailed")
            };

            await _dialogService.ShowDialogAsync(errorDialog);
        }
        finally
        {
            IsLoading = false;
            _isInitializationRunning = false;
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        ConfigurationViewRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanEditExercise))]
    private void EditExercise()
    {
        if (SelectedExerciseViewModel == null) return;

        var editor = new WellnessExerciseEditorViewModel(SelectedExerciseViewModel.Exercise);
        editor.Changed += OnEditorChanged;
        editor.Canceled += OnEditorCanceled;

        ContentViewModel = editor;
    }

    private void OnEditorChanged(object? sender, EventArgs e)
    {
        if (sender is not WellnessExerciseEditorViewModel editor) return;

        // an unplayable state (e.g. an instruction without duration) keeps the last playable one in the preview
        PreviewViewModel.UpdateExercise(editor.CreateRequest());
    }

    private void OnEditorCanceled(object? sender, EventArgs e)
    {
        CloseEditor();
        ShowExercise(SelectedExerciseViewModel);
    }

    private void CloseEditor()
    {
        if (Editor is not { } editor) return;

        editor.Changed -= OnEditorChanged;
        editor.Canceled -= OnEditorCanceled;
    }
    
    private void ShowExercise(WellnessExerciseViewModel? exercise)
    {
        ContentViewModel = exercise;

        // an exercise that cannot be played must not leave the previous one in the preview
        if (exercise == null || !PreviewViewModel.Load(exercise.Exercise, exercise.DefaultCycleCount))
        {
            PreviewViewModel.Clear();
        }
    }

    private async Task LoadWellnessExercises()
    {
        var exercises = await _wellnessExerciseRepository.GetWellnessExercisesAsync();
        var exerciseViewModels = exercises.Select(e => new WellnessExerciseViewModel(e));
        WellnessExercises.AddRange(exerciseViewModels);
        SelectedExerciseViewModel = WellnessExercises.FirstOrDefault();
    }
    
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        CloseEditor();
        PreviewViewModel.Dispose();
    }
}
