// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.ViewModels.DataEntries;

internal partial class NumericActivityDataEntryViewModel : ActivityDataEntryViewModel
{
    [ObservableProperty] private double _value;
    
    public NumericActivityDataEntryViewModel(NumericActivityDataEntry numericActivityEntry) : base(numericActivityEntry)
    {
        Value = numericActivityEntry.Value;
    }
}