// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Services;

namespace easpace.Desktop.Features.Activities.ViewModels;

public abstract partial class NumericActivityViewModel : ActivityViewModel
{
    [ObservableProperty] private string? _unit;
    [ObservableProperty] private double? _target;
    
    public NumericActivityViewModel(
        NumericActivity numericActivity,
        IDataEntryService dataEntryService) : base(numericActivity, dataEntryService)
    {
        Unit = numericActivity.Unit;
        Target = numericActivity.Target;
    }
}