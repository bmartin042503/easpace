// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models;

public partial class MoodEntry : ObservableObject
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    
    [ObservableProperty] private double _value;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private IList<MoodLabelState> _labels = [];
}
