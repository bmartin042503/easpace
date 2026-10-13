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

internal partial class WellnessExercisesViewModel : ViewModelBase
{
    private readonly IWellnessExerciseRepository _wellnessExerciseRepository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<WellnessExercisesViewModel> _logger;

    [ObservableProperty] private ObservableObject? _contentViewModel;

    [ObservableProperty] private WellnessExerciseViewModel? _selectedExerciseViewModel;

    [ObservableProperty] private bool _isLoading = true;
    private bool _isInitializationRunning;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowNoExercises))]
    private bool _isInitialized;
    
    [ObservableProperty] private string _title;

    public AvaloniaList<WellnessExerciseViewModel> WellnessExercises { get; set; } = [];

    public event EventHandler? ConfigurationViewRequested;

    public bool HasExercises => WellnessExercises.Count > 0;
    public bool ShowNoExercises => IsInitialized && !HasExercises;

    public WellnessExercisesViewModel(
        IWellnessExerciseRepository wellnessExerciseRepository,
        IDialogService dialogService,
        ILogger<WellnessExercisesViewModel> logger)
    {
        _wellnessExerciseRepository = wellnessExerciseRepository;
        _dialogService = dialogService;
        _logger = logger;
        
        Title = LocalizationService.GetString("Wellness.Button.Exercises");
        
        WellnessExercises.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasExercises));
            OnPropertyChanged(nameof(ShowNoExercises));
        };

        ContentViewModel = SelectedExerciseViewModel;
    }

    partial void OnSelectedExerciseViewModelChanged(WellnessExerciseViewModel? value)
    {
        ContentViewModel = value;
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

    private async Task LoadWellnessExercises()
    {
        var exercises = await _wellnessExerciseRepository.GetWellnessExercisesAsync();
        var exerciseViewModels = exercises.Select(e => new WellnessExerciseViewModel(e));
        WellnessExercises.AddRange(exerciseViewModels);
        SelectedExerciseViewModel = WellnessExercises.FirstOrDefault();
    }
}