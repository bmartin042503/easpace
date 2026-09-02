// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

internal class TrendActivityDataProvider : ITrendActivityDataProvider
{
    private const int MaxDataPoints = 60;

    public TrendChartData GetChartData(
        ChartTimeRange chartTimeRange,
        int intervalsBack,
        List<NumericActivityDataEntry> numericEntries)
    {
        if (intervalsBack < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalsBack), "Interval offset cannot be negative.");
        }

        var now = DateTimeOffset.Now;

        var (rangeStart, rangeEnd) = GetTimeRange(chartTimeRange, intervalsBack, now);

        var entries = chartTimeRange == ChartTimeRange.All
            ? numericEntries
            : numericEntries
                .Where(e =>
                    e.Timestamp >= rangeStart &&
                    e.Timestamp < rangeEnd)
                .ToList();

        var dataPoints = chartTimeRange switch
        {
            ChartTimeRange.Day => GetDayData(entries),
            ChartTimeRange.Week => GetWeekData(entries),
            ChartTimeRange.Month => GetMonthData(entries),
            ChartTimeRange.Year => GetYearData(entries),
            ChartTimeRange.All => GetAllData(entries),
            _ => []
        };

        return new TrendChartData(DataPoints: dataPoints, RangeStart: rangeStart, RangeEnd: rangeEnd);
    }

    private static (DateTimeOffset? Start, DateTimeOffset? End) GetTimeRange(
        ChartTimeRange chartTimeRange,
        int intervalsBack,
        DateTimeOffset now)
    {
        return chartTimeRange switch
        {
            ChartTimeRange.Day => (
                now.AddDays(-(intervalsBack + 1)),
                now.AddDays(-intervalsBack)
            ),

            ChartTimeRange.Week => (
                now.AddDays(-7 * (intervalsBack + 1)),
                now.AddDays(-7 * intervalsBack)
            ),

            ChartTimeRange.Month => (
                now.AddMonths(-(intervalsBack + 1)),
                now.AddMonths(-intervalsBack)
            ),

            ChartTimeRange.Year => (
                now.AddYears(-(intervalsBack + 1)),
                now.AddYears(-intervalsBack)
            ),

            _ => (null, null)
        };
    }

    private static List<TrendChartDataPoint> GetDayData(
        List<NumericActivityDataEntry> entries)
    {
        if (entries.Count <= MaxDataPoints)
        {
            return entries
                .OrderBy(e => e.Timestamp)
                .Select(e =>
                    new TrendChartDataPoint(
                        e.Timestamp,
                        e.Value))
                .ToList();
        }

        // if there is too much data for a single day, group by hour
        return entries
            .GroupBy(e =>
            {
                var local = e.Timestamp.ToLocalTime();

                return new DateTime(
                    local.Year,
                    local.Month,
                    local.Day,
                    local.Hour,
                    0,
                    0);
            })
            .Select(CreateAverageDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private static List<TrendChartDataPoint> GetWeekData(
        List<NumericActivityDataEntry> entries)
    {
        // week view: group by local calendar day and average
        return entries
            .GroupBy(e => e.Timestamp.ToLocalTime().Date)
            .Select(CreateAverageDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private static List<TrendChartDataPoint> GetMonthData(
        List<NumericActivityDataEntry> entries)
    {
        // month view: group by local calendar day and average
        return entries
            .GroupBy(e => e.Timestamp.ToLocalTime().Date)
            .Select(CreateAverageDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private static List<TrendChartDataPoint> GetYearData(
        List<NumericActivityDataEntry> entries)
    {
        // always get the daily average first to avoid same-day duplications
        var dailyData = entries
            .GroupBy(e => e.Timestamp.ToLocalTime().Date)
            .Select(CreateAverageDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (dailyData.Count <= MaxDataPoints)
        {
            return dailyData;
        }

        // if we still have more than 60 daily points, group by week
        return entries
            .GroupBy(e => GetStartOfWeek(e.Timestamp))
            .Select(CreateAverageDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private static List<TrendChartDataPoint> GetAllData(List<NumericActivityDataEntry> entries)
    {
        if (entries.Count == 0) return [];

        // all view: always get the daily average first
        var dailyData = entries
            .GroupBy(e => e.Timestamp.ToLocalTime().Date)
            .Select(CreateAverageDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (dailyData.Count <= MaxDataPoints)
        {
            return dailyData;
        }

        var totalDays =
            (entries.Max(e => e.Timestamp) -
             entries.Min(e => e.Timestamp)).TotalDays;

        if (totalDays > 730)
        {
            // > 2 years -> group by month
            return entries
                .GroupBy(e =>
                {
                    var local = e.Timestamp.ToLocalTime();

                    return (local.Year, local.Month);
                })
                .Select(CreateAverageDataPoint)
                .OrderBy(e => e.Timestamp)
                .ToList();
        }

        // <= 2 years -> group by week
        return entries
            .GroupBy(e => GetStartOfWeek(e.Timestamp))
            .Select(CreateAverageDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private static TrendChartDataPoint CreateAverageDataPoint<TKey>(IGrouping<TKey, NumericActivityDataEntry> group)
    {
        // place an aggregated value at the last real measurement of its bucket
        // instead of the artificial start of the day/week/month
        var timestamp = group.Max(e => e.Timestamp);
        var value = Math.Round(group.Average(e => e.Value), 1);

        return new TrendChartDataPoint(timestamp, value);
    }

    private static DateTime GetStartOfWeek(DateTimeOffset date)
    {
        var localDate = date.ToLocalTime();
        var diff =
            (7 + (localDate.DayOfWeek - DayOfWeek.Monday)) % 7;

        return localDate.AddDays(-diff).Date;
    }
}