// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Mood.Constants;
using easpace.Desktop.Features.Mood.Contracts;
using easpace.Desktop.Features.Mood.Entities;
using easpace.Desktop.Features.Mood.Services;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Mood.ViewModels;

internal partial class MoodPageViewModel : PageViewModel
{
    private readonly IMoodEntryService _moodEntryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MoodPageViewModel> _logger;

    private readonly List<MoodLabelViewModel> _allMoodLabels = [];

    public AvaloniaList<MoodEntryViewModel> MoodEntries { get; } = [];
    public AvaloniaList<MoodLabelViewModel> MoodLabels { get; } = [];

    [ObservableProperty] private string _moodStateText = string.Empty;
    [ObservableProperty] private string _description = string.Empty;

    // [ObservableProperty] private DateTime? _selectedDate = DateTime.Now;
    // [ObservableProperty] private TimeSpan? _selectedTime = DateTimeOffset.Now.TimeOfDay;
    // public DateTimeOffset MaxAllowedDate => DateTimeOffset.Now;

    public bool HasMoodEntries => MoodEntries.Count > 0;

    public bool IsLoadMoreButtonVisible => MoodLabels.Count < _allMoodLabels.Count;

    private bool _isInitialized;

    public double MoodSliderValue
    {
        get;
        set
        {
            if (value is < 0 or > 1) return;
            field = value;

            var localizedValue = value switch
            {
                < 0.2 => LocalizationService.GetString("Mood.SliderState.VeryUnpleasant"),
                < 0.4 => LocalizationService.GetString("Mood.SliderState.SlightlyUnpleasant"),
                < 0.6 => LocalizationService.GetString("Mood.SliderState.Neutral"),
                < 0.8 => LocalizationService.GetString("Mood.SliderState.SlightlyPleasant"),
                <= 1.0 => LocalizationService.GetString("Mood.SliderState.VeryPleasant"),
                _ => string.Empty
            };

            MoodStateText = localizedValue;

            UpdateMoodLabels();

            OnPropertyChanged();
        }
    }

    public MoodPageViewModel(
        IMoodEntryService moodEntryService,
        IDialogService dialogService,
        ILogger<MoodPageViewModel> logger)
    {
        _moodEntryService = moodEntryService;
        _dialogService = dialogService;
        _logger = logger;

        Page = ApplicationPage.Mood;

        MoodLabels.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsLoadMoreButtonVisible));
        MoodEntries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMoodEntries));

        InitializeMoodLabels();
        MoodSliderValue = 0.5;
    }

    private void InitializeMoodLabels()
    {
        foreach (var labelState in Enum.GetValues<MoodLabelState>())
        {
            var localizedName = LocalizationService.GetString($"Mood.Label.{labelState}");
            _allMoodLabels.Add(new MoodLabelViewModel(labelState, localizedName));
        }
    }

    private void UpdateMoodLabels()
    {
        if (_allMoodLabels.Count <= 0) return;

        var currentState = MoodSliderValue switch
        {
            < 0.2 => MoodState.VeryUnpleasant,
            < 0.4 => MoodState.SlightlyUnpleasant,
            < 0.6 => MoodState.Neutral,
            < 0.8 => MoodState.SlightlyPleasant,
            _ => MoodState.VeryPleasant
        };

        var labelsToShow = _allMoodLabels
            .Where(l => l.IsChecked || l.State.BelongsTo(currentState))
            .ToList();

        if (MoodLabels.SequenceEqual(labelsToShow))
        {
            return;
        }

        MoodLabels.Clear();
        MoodLabels.AddRange(labelsToShow);
    }

    private async Task LoadEntries()
    {
        MoodEntries.Clear();

        var moodEntries = await _moodEntryService.GetMoodEntriesAsync();

        var moodEntryViewModels = moodEntries.Select(entry => new MoodEntryViewModel(entry))
            .ToList();

        MoodEntries.AddRange(moodEntryViewModels);

        OnPropertyChanged(nameof(HasMoodEntries));
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            await LoadEntries();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize mood page and load entries");
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Mood.Error.LoadFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    [RelayCommand]
    private void LoadAllLabels()
    {
        MoodLabels.Clear();
        MoodLabels.AddRange(_allMoodLabels);
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            var timestamp = DateTimeOffset.Now;

            var selectedLabels = _allMoodLabels
                .Where(l => l.IsChecked)
                .Select(l => l.State)
                .ToList();

            var createEntryRequest = new UpsertMoodEntryRequest(
                Timestamp: timestamp,
                Value: MoodSliderValue,
                Labels: selectedLabels,
                Description: Description
            );

            _logger.LogInformation("Saving new mood entry from UI");
            var savedEntry = await _moodEntryService.CreateMoodEntryAsync(createEntryRequest);

            var entryViewModel = new MoodEntryViewModel(savedEntry);

            MoodEntries.Insert(0, entryViewModel);

            ResetForm();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save mood entry from UI");
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Mood.Error.SaveFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    [RelayCommand]
    private async Task DeleteEntry(object? parameter)
    {
        if (parameter is not MoodEntryViewModel entryViewModel) return;

        try
        {
            var deleted = await _moodEntryService.DeleteMoodEntryAsync(entryViewModel.Id);

            if (deleted)
            {
                MoodEntries.Remove(entryViewModel);
                _logger.LogInformation("Mood entry {EntryId} removed from UI successfully", entryViewModel.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete mood entry {EntryId} from UI", entryViewModel.Id);
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Mood.Error.DeleteFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    private void ResetForm()
    {
        Description = string.Empty;

        foreach (var label in _allMoodLabels)
        {
            label.IsChecked = false;
        }

        MoodSliderValue = 0.5;
    }
}