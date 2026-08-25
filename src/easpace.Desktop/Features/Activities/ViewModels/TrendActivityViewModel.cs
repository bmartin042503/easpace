// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Controls;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal partial class TrendActivityViewModel : NumericActivityViewModel
{
    private readonly TrendActivity _trendActivity;
    private readonly ITrendActivityDataProvider _trendActivityDataProvider;
    private readonly IActivityService _activityService;

    public IEnumerable<ChartTimeRange> TimeRanges { get; } = Enum.GetValues<ChartTimeRange>();

    [ObservableProperty] private ChartTimeRange _selectedTimeRange;
    [ObservableProperty] private IEnumerable<TrendChartDataPoint> _dataPoints = [];

    public NumericActivityDataEntryViewModel? LastEntry => Entries.OfType<NumericActivityDataEntryViewModel>().MaxBy(e => e.Timestamp);

    public string CurrentValueText => string.Format(LocalizationService.GetString("TrendActivity.Details.CurrentValue"),
        LastEntry?.Value, Unit);

    public TrendActivityViewModel(
        TrendActivity trendActivity,
        ITrendActivityDataProvider trendActivityDataProvider,
        IActivityDataEntryService activityDataEntryService,
        IActivityService activityService,
        IDialogService dialogService) : base(trendActivity, activityDataEntryService, dialogService)
    {
        _trendActivity = trendActivity;
        _trendActivityDataProvider = trendActivityDataProvider;
        _activityService = activityService;

        _selectedTimeRange = ChartTimeRange.Day;
        
        LoadEntries();
    }

    public override async Task<Activity?> UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = await _activityService.UpdateActivityAsync(Id, updateRequest);

        if (updated is null) return null;

        Name = ((TrendActivity)updated).Name;
        Unit = ((TrendActivity)updated).Unit;
        Target = ((TrendActivity)updated).Target;

        return updated;
    }

    partial void OnSelectedTimeRangeChanged(ChartTimeRange value)
    {
        UpdateDataPoints(value);
    }

    private void UpdateDataPoints(ChartTimeRange timeRange)
    {
        var numericEntries = _trendActivity.Entries.OfType<NumericActivityDataEntry>().ToList();
        DataPoints = _trendActivityDataProvider.GetChartData(timeRange, numericEntries);
    }

    protected override void OnEntryCollectionChanged()
    {
        base.OnEntryCollectionChanged();
        OnPropertyChanged(nameof(LastEntry));
        OnPropertyChanged(nameof(CurrentValueText));
        UpdateDataPoints(SelectedTimeRange);
    }
}