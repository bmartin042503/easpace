// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Validation;
using easpace.Desktop.Validation;

namespace easpace.Desktop.Features.Activities.ViewModels.Dialogs;

internal partial class NumericEntryDialogViewModel : EntryDialogViewModel
{
    [ObservableProperty] private string? _unitText = string.Empty;
    
    [ObservableProperty] private double? _numericValue;

    public NumericEntryDialogViewModel()
    {
        Validate();
    }

    partial void OnNumericValueChanged(double? value) => Validate();

    protected override IEnumerable<ValidationIssue> GetValidationErrors() => ActivityEntryValidator.Validate(SelectedDate, NumericValue);
}
