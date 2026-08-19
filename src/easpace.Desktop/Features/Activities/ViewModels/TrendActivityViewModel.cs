// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Linq;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class TrendActivityViewModel : NumericActivityViewModel
{
    private readonly TrendActivity _trendActivity;
    private readonly ITrendActivityDataProvider _trendActivityDataProvider;
    private readonly IActivityService _activityService;

    [ObservableProperty] private ChartTimeRange _selectedTimeRange;

    public AvaloniaList<TrendChartDataPoint> DataPoints { get; } = [];

    public TrendActivityViewModel(
        TrendActivity trendActivity,
        ITrendActivityDataProvider trendActivityDataProvider,
        IDataEntryService dataEntryService,
        IActivityService activityService) : base(trendActivity, dataEntryService)
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
        UpdateVisibleEntries(value);
    }

    private void UpdateVisibleEntries(ChartTimeRange timeRange)
    {
        DataPoints.Clear();

        var numericEntries = _trendActivity.Entries.OfType<NumericDataEntry>().ToList();

        var dataPoints = _trendActivityDataProvider.GetChartData(timeRange, numericEntries);

        DataPoints.AddRange(dataPoints);
    }
}