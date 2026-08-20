// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
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

namespace easpace.Desktop.Features.Mood.ViewModels;

public partial class MoodPageViewModel : PageViewModel
{
    private readonly IMoodEntryService _moodEntryService;

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

    public MoodPageViewModel(IMoodEntryService moodEntryService)
    {
        _moodEntryService = moodEntryService;

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

    [RelayCommand]
    private void LoadAllLabels()
    {
        MoodLabels.Clear();
        MoodLabels.AddRange(_allMoodLabels);
    }

    [RelayCommand]
    private void Save()
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
        
        var savedEntry = _moodEntryService.CreateMoodEntry(createEntryRequest);

        var entryViewModel = new MoodEntryViewModel(savedEntry);

        MoodEntries.Insert(0, entryViewModel);

        ResetForm();
    }

    [RelayCommand]
    private void DeleteEntry(object? parameter)
    {
        if (parameter is not MoodEntryViewModel entryViewModel) return;

        var deleted = _moodEntryService.DeleteMoodEntry(entryViewModel.Id);

        if (deleted)
        {
            MoodEntries.Remove(entryViewModel);
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