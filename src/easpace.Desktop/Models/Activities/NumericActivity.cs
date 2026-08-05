// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Models.Activities;

public abstract partial class NumericActivity : Activity<NumericDataEntry>
{
    [ObservableProperty] private double? _targetValue;
    [ObservableProperty] private string _unit = string.Empty;
    [ObservableProperty] private DateTime _targetDate;
}