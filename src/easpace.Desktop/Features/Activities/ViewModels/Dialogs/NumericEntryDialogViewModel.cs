// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Features.Activities.ViewModels.Dialogs;

internal partial class NumericEntryDialogViewModel : EntryDialogViewModel
{
    [ObservableProperty] private string? _unitText = string.Empty;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private double? _numericValue;

    protected override bool CanConfirm()
    {
        return base.CanConfirm() && NumericValue.HasValue;
    }
}