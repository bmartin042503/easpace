// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
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
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    private DateTimeOffset _timestamp;

    public AvaloniaList<MoodLabelState> Labels { get; } = [];
    
    public bool HasLabels => Labels.Count > 0;

    public string TimestampText => Timestamp.ToLocalTime().ToString("F", CultureInfo.CurrentCulture);
    
    public MoodEntryViewModel(MoodEntry moodEntry)
    {
        Id = moodEntry.Id;
        Timestamp = moodEntry.Timestamp;
        Value = moodEntry.Value;
        Description = moodEntry.Description;
        Labels.AddRange(moodEntry.Labels);
        
        Labels.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasLabels));
    }
}