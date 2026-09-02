// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;
using easpace.Desktop.Features.Activities.ViewModels.DataEntries;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;

namespace easpace.Desktop.Features.Activities.ViewModels;

internal partial class TrendActivityViewModel : NumericActivityViewModel
{
    private readonly TrendActivity _trendActivity;
    private readonly ITrendActivityDataProvider _trendActivityDataProvider;
    private readonly IActivityService _activityService;

    private int _intervalsBack;

    public IEnumerable<ChartTimeRange> TimeRanges { get; } = Enum.GetValues<ChartTimeRange>();

    [ObservableProperty] private ChartTimeRange _selectedTimeRange;
    [ObservableProperty] private IEnumerable<TrendChartDataPoint> _dataPoints = [];
    
    [ObservableProperty] private string _visibleRangeText = string.Empty;
    [ObservableProperty] private DateTimeOffset? _visibleRangeStart;
    [ObservableProperty] private DateTimeOffset? _visibleRangeEnd;

    public int IntervalsBack => _intervalsBack;

    public bool CanNavigateIntervals => SelectedTimeRange != ChartTimeRange.All;

    public NumericActivityDataEntryViewModel? LastEntry =>
        Entries.OfType<NumericActivityDataEntryViewModel>().MaxBy(e => e.Timestamp);

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

        UpdateDataPoints();
    }

    public override async Task<Activity?> UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = await _activityService.UpdateActivityAsync(Id, updateRequest);

        if (updated is not TrendActivity trendActivity)
            return null;

        Name = trendActivity.Name;
        Unit = trendActivity.Unit;
        Target = trendActivity.Target;

        return updated;
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedTimeRangeChanged(ChartTimeRange value)
    {
        // changing the range always returns to its current interval
        _intervalsBack = 0;

        OnPropertyChanged(nameof(IntervalsBack));
        OnPropertyChanged(nameof(CanNavigateIntervals));

        UpdateDataPoints();

        PreviousIntervalCommand.NotifyCanExecuteChanged();
        NextIntervalCommand.NotifyCanExecuteChanged();
    }
    
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnVisibleRangeStartChanged(DateTimeOffset? value)
    {
        UpdateVisibleRangeText();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnVisibleRangeEndChanged(DateTimeOffset? value)
    {
        UpdateVisibleRangeText();
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousInterval))]
    private void PreviousInterval()
    {
        _intervalsBack++;
        OnPropertyChanged(nameof(IntervalsBack));
        UpdateDataPoints();

        PreviousIntervalCommand.NotifyCanExecuteChanged();
        NextIntervalCommand.NotifyCanExecuteChanged();
    }

    private bool CanGoToPreviousInterval()
    {
        if (SelectedTimeRange == ChartTimeRange.All || !VisibleRangeStart.HasValue) return false;

        // do not allow navigating indefinitely into intervals before the first existing entry
        return Entries.OfType<NumericActivityDataEntryViewModel>()
            .Any(e => e.Timestamp < VisibleRangeStart.Value);
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextInterval))]
    private void NextInterval()
    {
        if (_intervalsBack <= 0) return;
        
        _intervalsBack--;
        
        OnPropertyChanged(nameof(IntervalsBack));
        
        UpdateDataPoints();
        
        PreviousIntervalCommand.NotifyCanExecuteChanged();
        NextIntervalCommand.NotifyCanExecuteChanged();
    }

    private bool CanGoToNextInterval()
    {
        return SelectedTimeRange != ChartTimeRange.All && _intervalsBack > 0;
    }

    private void UpdateDataPoints()
    {
        var numericEntries = _trendActivity.Entries.OfType<NumericActivityDataEntry>().ToList();
        
        var chartData = _trendActivityDataProvider.GetChartData(
            SelectedTimeRange,
            _intervalsBack,
            numericEntries);

        DataPoints = chartData.DataPoints;
        VisibleRangeStart = chartData.RangeStart;
        VisibleRangeEnd = chartData.RangeEnd;
        
        PreviousIntervalCommand.NotifyCanExecuteChanged();
        NextIntervalCommand.NotifyCanExecuteChanged();
    }
    
    private void UpdateVisibleRangeText()
    {
        if (!VisibleRangeStart.HasValue || !VisibleRangeEnd.HasValue || SelectedTimeRange == ChartTimeRange.All)
        {
            VisibleRangeText = string.Empty;
            return;
        }

        var start = VisibleRangeStart.Value.ToLocalTime();
        var end = VisibleRangeEnd.Value.ToLocalTime();

        VisibleRangeText = SelectedTimeRange switch
        {
            ChartTimeRange.Day => FormatDayRange(start, end),
            _ => FormatDateRange(start, end)
        };
    }
    
    private static string FormatDateRange(DateTimeOffset start, DateTimeOffset end)
    {
        var culture = CultureInfo.CurrentUICulture;

        if (start.Year != end.Year)
        {
            return
                $"{start.ToString("yyyy. MMMM d.", culture)} - " +
                $"{end.ToString("yyyy. MMMM d.", culture)}";
        }

        if (start.Month != end.Month)
        {
            return
                $"{start.ToString("yyyy. MMMM d.", culture)} - " +
                $"{end.ToString("MMMM d.", culture)}";
        }

        if (start.Date != end.Date)
        {
            return
                $"{start.ToString("yyyy. MMMM d.", culture)} - " +
                $"{end.ToString("d.", culture)}";
        }

        return start.ToString("yyyy. MMMM d.", culture);
    }
    
    private static string FormatDayRange(DateTimeOffset start, DateTimeOffset end)
    {
        var culture = CultureInfo.CurrentUICulture;

        return
            $"{start.ToString("yyyy. MMMM d. HH:mm", culture)} - " +
            $"{end.ToString("MMMM d. HH:mm", culture)}";
    }

    protected override void OnEntryCollectionChanged()
    {
        base.OnEntryCollectionChanged();
        RefreshData();
    }

    protected override void OnDataEntryUpdated()
    {
        base.OnDataEntryUpdated();
        RefreshData();
    }

    private void RefreshData()
    {
        OnPropertyChanged(nameof(LastEntry));
        OnPropertyChanged(nameof(CurrentValueText));
        
        UpdateDataPoints();
    }
}