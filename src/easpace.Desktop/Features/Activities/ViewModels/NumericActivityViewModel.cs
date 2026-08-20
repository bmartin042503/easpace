// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Features.Activities.ViewModels.Dialogs;
using easpace.Desktop.Services;

namespace easpace.Desktop.Features.Activities.ViewModels;

public abstract partial class NumericActivityViewModel : ActivityViewModel
{
    private readonly NumericActivity _numericActivity;
    private readonly IDataEntryService _dataEntryService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private string? _unit;
    [ObservableProperty] private double? _target;

    public NumericActivityViewModel(
        NumericActivity numericActivity,
        IDataEntryService dataEntryService,
        IDialogService dialogService) : base(numericActivity, dataEntryService)
    {
        _numericActivity = numericActivity;
        _dataEntryService = dataEntryService;
        _dialogService = dialogService;
        Unit = numericActivity.Unit;
        Target = numericActivity.Target;
    }

    [RelayCommand]
    private async Task AddDataEntry()
    {
        var numericEntryDialog = new NumericEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            SelectedDate = DateTime.Now
        };

        await _dialogService.ShowDialogAsync(numericEntryDialog);

        if (numericEntryDialog is { Confirmed: true, NumericValue: not null })
        {
            var createEntryRequest = new CreateDataEntryRequest(
                Timestamp: numericEntryDialog.GetTimestamp(),
                Value: numericEntryDialog.NumericValue,
                State: null,
                Type: DataEntryType.Numeric
            );

            var dataEntry = _dataEntryService.CreateDataEntry(Id, createEntryRequest);

            if (dataEntry is not NumericDataEntry numericDataEntry) return;
            
            _numericActivity.Entries.Add(numericDataEntry);

            var dataEntryVm = new NumericDataEntryViewModel(numericDataEntry);

            Entries.Add(dataEntryVm);
        }
    }

    public override async Task<DataEntryViewModel?> EditDataEntry(Guid entryId)
    {
        var entryVm = Entries.OfType<NumericDataEntryViewModel>().FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return null;

        var numericEntryDialog = new NumericEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            NumericValue = entryVm.Value,
            SelectedDate = entryVm.Timestamp.Date
        };

        await _dialogService.ShowDialogAsync(numericEntryDialog);

        if (numericEntryDialog is { Confirmed: true, NumericValue: not null })
        {
            var updateRequest = new UpdateDataEntryRequest(
                Timestamp: numericEntryDialog.GetTimestamp(),
                Value: numericEntryDialog.NumericValue,
                State: null
            );

            var updatedEntry = _dataEntryService.UpdateDataEntry(entryId, updateRequest);

            if (updatedEntry is not NumericDataEntry numericDataEntry) return null;

            if (updateRequest.Timestamp is not null)
            {
                entryVm.Timestamp = numericDataEntry.Timestamp;
            }

            if (updateRequest.Value is not null)
            {
                entryVm.Value = numericDataEntry.Value;
            }
        }

        return entryVm;
    }
}