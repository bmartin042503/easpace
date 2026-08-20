// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
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

public partial class TrendActivityViewModel : NumericActivityViewModel
{
    private readonly TrendActivity _trendActivity;
    private readonly ITrendActivityDataProvider _trendActivityDataProvider;
    private readonly IActivityService _activityService;

    public IEnumerable<ChartTimeRange> TimeRanges { get; } = Enum.GetValues<ChartTimeRange>();

    [ObservableProperty] private ChartTimeRange _selectedTimeRange;
    [ObservableProperty] private IEnumerable<TrendChartDataPoint> _dataPoints = [];

    public NumericDataEntryViewModel? LastEntry => Entries.OfType<NumericDataEntryViewModel>().MaxBy(e => e.Timestamp);

    public string CurrentValueText => string.Format(LocalizationService.GetString("TrendActivity.Details.CurrentValue"),
        LastEntry?.Value, Unit);

    public TrendActivityViewModel(
        TrendActivity trendActivity,
        ITrendActivityDataProvider trendActivityDataProvider,
        IDataEntryService dataEntryService,
        IActivityService activityService,
        IDialogService dialogService) : base(trendActivity, dataEntryService, dialogService)
    {
        _trendActivity = trendActivity;
        _trendActivityDataProvider = trendActivityDataProvider;
        _activityService = activityService;

        _selectedTimeRange = ChartTimeRange.Day;
    }

    public override Activity? UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = _activityService.UpdateActivity(Id, updateRequest);

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
        var numericEntries = _trendActivity.Entries.OfType<NumericDataEntry>().ToList();
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