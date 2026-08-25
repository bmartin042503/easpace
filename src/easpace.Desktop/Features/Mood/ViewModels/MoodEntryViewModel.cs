// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Mood.Constants;
using easpace.Desktop.Features.Mood.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Mood.ViewModels;

internal partial class MoodEntryViewModel : ViewModelBase
{
    public Guid Id { get; }

    [ObservableProperty] private double _value;
    [ObservableProperty] private string _description;
    [ObservableProperty] private DateTimeOffset _timestamp;

    public AvaloniaList<MoodLabelState> Labels { get; } = [];
    
    public MoodEntryViewModel(MoodEntry moodEntry)
    {
        Id = moodEntry.Id;
        Timestamp = moodEntry.Timestamp;
        Value = moodEntry.Value;
        Description = moodEntry.Description;
        Labels.AddRange(moodEntry.Labels);
    }
}