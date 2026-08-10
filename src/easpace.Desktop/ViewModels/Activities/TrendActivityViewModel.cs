// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public partial class TrendActivityViewModel : ActivityViewModel
{
    [ObservableProperty] private IEnumerable<NumericDataEntry> _chartEntries = [];

    public IEnumerable<ChartTimeRange> TimeRanges { get; } = Enum.GetValues<ChartTimeRange>();

    public ChartTimeRange SelectedTimeRange
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            UpdateChartData();
        }
    }

    public TrendActivity Activity => (TrendActivity)BaseActivity;

    public TrendActivityViewModel(TrendActivity activity)
    {
        BaseActivity = activity;
        
        Activity.Entries.CollectionChanged += OnEntriesCollectionChanged;
        
        SelectedTimeRange = ChartTimeRange.Month;
    }
    
    // empty constructor for AXAML preview
    public TrendActivityViewModel() {}
    
    private void UpdateChartData()
    {
        if (Activity.Entries.Count == 0)
        {
            ChartEntries = [];
            return;
        }

        var now = DateTime.Now;

        // optimal maximum points on the chart before we start aggregating
        const int maxPoints = 60;

        switch (SelectedTimeRange)
        {
            case ChartTimeRange.Day:
                var dayData = Activity.Entries.Where(e => e.Timestamp >= now.AddDays(-1)).ToList();
                if (dayData.Count > maxPoints)
                {
                    // if there is too much data in a single day, we group by hour
                    ChartEntries = dayData
                        .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0))
                        .Select(g => new NumericDataEntry
                            { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else 
                {
                    // day view: keep the raw, exact data
                    ChartEntries = dayData.OrderBy(e => e.Timestamp).ToList();
                }
                break;

            case ChartTimeRange.Week:
                var weekData = Activity.Entries.Where(e => e.Timestamp >= now.AddDays(-7)).ToList();
                
                // week view: maximum 7 days., always group by day and average, no need for maxPoints check
                ChartEntries = weekData
                    .GroupBy(e => e.Timestamp.Date) 
                    .Select(g => new NumericDataEntry
                        { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                    .OrderBy(e => e.Timestamp).ToList();
                break;

            case ChartTimeRange.Month:
                var monthData = Activity.Entries.Where(e => e.Timestamp >= now.AddMonths(-1)).ToList();
                
                // month view: maximum 31 days, always group by day and average, no need for maxPoints check
                ChartEntries = monthData
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new NumericDataEntry
                        { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                    .OrderBy(e => e.Timestamp).ToList();
                break;

            case ChartTimeRange.Year:
                var yearData = Activity.Entries.Where(e => e.Timestamp >= now.AddYears(-1)).ToList();
                
                // year view: always get the daily average first to avoid same-day duplications
                var dailyYearData = yearData
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new NumericDataEntry
                        { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                    .ToList();

                if (dailyYearData.Count > maxPoints)
                {
                    // if we still have more than 60 points daily, we group by week
                    ChartEntries = yearData
                        .GroupBy(e => GetStartOfWeek(e.Timestamp))
                        .Select(g => new NumericDataEntry
                            { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else 
                {
                    // if it fits within the 60 points limit, we show the daily averaged data (not the raw)
                    ChartEntries = dailyYearData.OrderBy(e => e.Timestamp).ToList();
                }
                break;

            case ChartTimeRange.All:
                var allData = Activity.Entries.ToList();
                
                // all view: always get the daily average first
                var dailyAllData = allData
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new NumericDataEntry
                        { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                    .ToList();

                if (dailyAllData.Count > maxPoints)
                {
                    var totalDays = (allData.Max(e => e.Timestamp) - allData.Min(e => e.Timestamp)).TotalDays;

                    if (totalDays > 730) 
                    {
                        // > 2 years -> group by month
                        ChartEntries = allData
                            .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, 1))
                            .Select(g => new NumericDataEntry
                                { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                            .OrderBy(e => e.Timestamp).ToList();
                    }
                    else 
                    {
                        // < 2 years -> group by week
                        ChartEntries = allData
                            .GroupBy(e => GetStartOfWeek(e.Timestamp))
                            .Select(g => new NumericDataEntry
                                { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                            .OrderBy(e => e.Timestamp).ToList();
                    }
                }
                else 
                {
                    // if it fits within the 60 points limit, we show the daily averaged data
                    ChartEntries = dailyAllData.OrderBy(e => e.Timestamp).ToList();
                }
                break;
        }
    }

    /// <summary>
    /// Helper method to find the Monday of the week for a given date
    /// </summary>
    private static DateTime GetStartOfWeek(DateTime dt)
    {
        var diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
    
    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateChartData();
    }
}