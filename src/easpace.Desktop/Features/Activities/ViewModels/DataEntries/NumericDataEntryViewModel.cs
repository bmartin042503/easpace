// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.ViewModels.DataEntries;

public partial class NumericDataEntryViewModel : DataEntryViewModel
{
    [ObservableProperty] private double _value;
    
    public NumericDataEntryViewModel(NumericDataEntry numericEntry) : base(numericEntry)
    {
        Value = numericEntry.Value;
    }
}