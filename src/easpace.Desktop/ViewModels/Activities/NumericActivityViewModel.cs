// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Models.Activities;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels.Activities;

public abstract partial class NumericActivityViewModel<TActivity> : ActivityViewModel
    where TActivity : NumericActivity
{
    private readonly IDialogService _dialogService;
    
    public TActivity Activity => (TActivity)BaseActivity;
    
    public NumericActivityViewModel(NumericActivity activity, IDialogService dialogService)
    {
        BaseActivity = activity;
        
        _dialogService = dialogService;
    }
    
    [RelayCommand]
    private async Task AddNumericEntry()
    {
        var numericEntryDialog = new NumericEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            UnitText = string.IsNullOrEmpty(Activity.Unit) ? null : Activity.Unit,
            SelectedDate = DateTime.Now
        };

        await _dialogService.ShowDialogAsync(numericEntryDialog);

        if (numericEntryDialog is { Confirmed: true, NumericValue: not null })
        {
            var numericValue = numericEntryDialog.NumericValue.Value;
            var numericEntry = new NumericDataEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = numericEntryDialog.GetTimestamp(),
                Value = numericValue
            };
            Activity.Entries.Add(numericEntry);
        }
    }
}