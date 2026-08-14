// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;
using easpace.Desktop.Services;

namespace easpace.Desktop.ViewModels;

public partial class MoodViewModel : PageViewModel
{
    private readonly List<MoodLabelViewModel> _allMoodLabels = [];

    public ObservableCollection<MoodEntry> MoodEntries { get; } = [];
    public ObservableCollection<MoodLabelViewModel> MoodLabels { get; } = [];

    [ObservableProperty] private string _moodStateText = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    
    // [ObservableProperty] private DateTime? _selectedDate = DateTime.Now;
    // [ObservableProperty] private TimeSpan? _selectedTime = DateTimeOffset.Now.TimeOfDay;
    
    public bool HasMoodEntries => MoodEntries.Count > 0;

    public DateTimeOffset MaxAllowedDate => DateTimeOffset.Now;
    
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

    public MoodViewModel()
    {
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
        foreach (var label in labelsToShow)
        {
            MoodLabels.Add(label);
        }
    }

    [RelayCommand]
    private void LoadAllLabels()
    {
        MoodLabels.Clear();
        foreach (var label in _allMoodLabels)
        {
            MoodLabels.Add(label);
        }
    }

    [RelayCommand]
    private void Save()
    {
        var timestamp = DateTimeOffset.Now;

        var selectedLabels = _allMoodLabels
            .Where(l => l.IsChecked)
            .Select(l => l.State)
            .ToList();

        var entry = new MoodEntry
        {
            Timestamp = timestamp,
            Value = MoodSliderValue,
            Labels = selectedLabels,
            Description = Description
        };

        MoodEntries.Insert(0, entry);

        ResetForm();
    }
    
    [RelayCommand]
    private void DeleteEntry(object? parameter)
    {
        if (parameter is not MoodEntry entry) return;
        
        MoodEntries.Remove(entry);
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