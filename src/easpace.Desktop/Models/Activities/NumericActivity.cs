// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Models.Activities;

public abstract partial class NumericActivity : Activity<NumericDataEntry>
{
    [ObservableProperty] private double? _target;
    [ObservableProperty] private string? _unit = string.Empty;
}