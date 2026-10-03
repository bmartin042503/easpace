// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Repositories;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Features.Activities.ViewModels.Dialogs;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal abstract partial class NumericActivityViewModel : ActivityViewModel
{
    private readonly NumericActivity _numericActivity;
    private readonly IActivityDataEntryRepository _activityDataEntryRepository;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ActivityViewModel> _logger;

    [ObservableProperty] private string? _unit;
    [ObservableProperty] private double? _target;

    public override ICommand AddDataEntryCommand { get; }

    public NumericActivityViewModel(
        NumericActivity numericActivity,
        IActivityDataEntryRepository activityDataEntryRepository,
        IDialogService dialogService,
        ILogger<ActivityViewModel> logger) : base(numericActivity, activityDataEntryRepository)
    {
        _numericActivity = numericActivity;
        _activityDataEntryRepository = activityDataEntryRepository;
        _dialogService = dialogService;
        _logger = logger;

        AddDataEntryCommand = new AsyncRelayCommand(AddDataEntryAsync);
        
        Unit = numericActivity.Unit;
        Target = numericActivity.Target;
    }
    
    private async Task AddDataEntryAsync()
    {
        var numericEntryDialog = new NumericEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            SelectedDate = DateTime.Now
        };

        await _dialogService.ShowDialogAsync(numericEntryDialog);

        if (numericEntryDialog is not { Confirmed: true, NumericValue: not null }) return;

        try
        {
            var createEntryRequest = new CreateDataEntryRequest(
                Timestamp: numericEntryDialog.GetTimestamp(),
                Value: numericEntryDialog.NumericValue,
                State: null,
                Type: ActivityDataEntryType.Numeric
            );

            var dataEntry = await _activityDataEntryRepository.CreateDataEntryAsync(Id, createEntryRequest);

            if (dataEntry is not NumericActivityDataEntry numericDataEntry) return;

            // keep the local entity collection synchronized if EF has not already done so
            if (_numericActivity.Entries.All(e => e.Id != numericDataEntry.Id))
            {
                _numericActivity.Entries.Add(numericDataEntry);
            }

            var dataEntryVm = new NumericActivityDataEntryViewModel(numericDataEntry);

            Entries.Insert(0, dataEntryVm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add data entry to activity {ActivityId}", Id);

            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Activities.Error.EntrySaveFailed")
            };

            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    public override async Task<ActivityDataEntryViewModel?> EditDataEntry(Guid entryId)
    {
        var entryVm = Entries.OfType<NumericActivityDataEntryViewModel>().FirstOrDefault(e => e.Id == entryId);
        if (entryVm is null) return null;

        var numericEntryDialog = new NumericEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EditEntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            NumericValue = entryVm.Value,
            SelectedDate = entryVm.Timestamp.Date,
            SelectedTime = entryVm.Timestamp.TimeOfDay
        };

        await _dialogService.ShowDialogAsync(numericEntryDialog);

        if (numericEntryDialog is { Confirmed: true, NumericValue: not null })
        {
            var updateRequest = new UpdateDataEntryRequest(
                Timestamp: numericEntryDialog.GetTimestamp(),
                Value: numericEntryDialog.NumericValue,
                State: null
            );

            var updatedEntry = await _activityDataEntryRepository.UpdateDataEntryAsync(entryId, updateRequest);

            if (updatedEntry is not NumericActivityDataEntry numericDataEntry) return null;

            var entityEntry = _numericActivity.Entries
                .OfType<NumericActivityDataEntry>()
                .FirstOrDefault(e => e.Id == entryId);

            if (entityEntry is not null)
            {
                entityEntry.Timestamp = numericDataEntry.Timestamp;
                entityEntry.Value = numericDataEntry.Value;
            }

            entryVm.Timestamp = numericDataEntry.Timestamp;
            entryVm.Value = numericDataEntry.Value;

            OnDataEntryUpdated();
        }

        return entryVm;
    }

    protected virtual void OnDataEntryUpdated() {}
}