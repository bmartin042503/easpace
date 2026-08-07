// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
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
                    // group by hour
                    ChartEntries = dayData
                        .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day))
                        .Select(g => new NumericDataEntry
                            { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else ChartEntries = dayData.OrderBy(e => e.Timestamp).ToList();

                break;

            case ChartTimeRange.Week:
                var weekData = Activity.Entries.Where(e => e.Timestamp >= now.AddDays(-7)).ToList();
                if (weekData.Count > maxPoints)
                {
                    // group by day
                    ChartEntries = weekData
                        .GroupBy(e => e.Timestamp.Date) // .Date gives midnight of that day
                        .Select(g => new NumericDataEntry
                            { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else ChartEntries = weekData.OrderBy(e => e.Timestamp).ToList();

                break;

            case ChartTimeRange.Month:
                var monthData = Activity.Entries.Where(e => e.Timestamp >= now.AddMonths(-1)).ToList();
                if (monthData.Count > maxPoints)
                {
                    // group by day
                    ChartEntries = monthData
                        .GroupBy(e => e.Timestamp.Date)
                        .Select(g => new NumericDataEntry
                            { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else ChartEntries = monthData.OrderBy(e => e.Timestamp).ToList();

                break;

            case ChartTimeRange.Year:
                var yearData = Activity.Entries.Where(e => e.Timestamp >= now.AddYears(-1)).ToList();
                if (yearData.Count > maxPoints)
                {
                    // group by week
                    ChartEntries = yearData
                        .GroupBy(e => GetStartOfWeek(e.Timestamp))
                        .Select(g => new NumericDataEntry
                            { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else ChartEntries = yearData.OrderBy(e => e.Timestamp).ToList();

                break;

            case ChartTimeRange.All:
                var allData = Activity.Entries.ToList();
                if (allData.Count > maxPoints)
                {
                    var totalDays = (allData.Max(e => e.Timestamp) - allData.Min(e => e.Timestamp)).TotalDays;

                    if (totalDays > 730) // > 2 years -> group by Month
                    {
                        ChartEntries = allData
                            .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, 1))
                            .Select(g => new NumericDataEntry
                                { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                            .OrderBy(e => e.Timestamp).ToList();
                    }
                    else // < 2 years -> group by Week
                    {
                        ChartEntries = allData
                            .GroupBy(e => GetStartOfWeek(e.Timestamp))
                            .Select(g => new NumericDataEntry
                                { Timestamp = g.Key, Value = Math.Round(g.Average(x => x.Value), 1) })
                            .OrderBy(e => e.Timestamp).ToList();
                    }
                }
                else ChartEntries = allData.OrderBy(e => e.Timestamp).ToList();

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
}