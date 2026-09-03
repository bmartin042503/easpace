// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

/// <summary>
/// Builds chart data for trend activities and applies daily aggregation and visual downsampling when needed.
/// </summary>
internal class TrendActivityDataProvider : ITrendActivityDataProvider
{
    private const int MaxDataPoints = 60;

    /// <summary>
    /// Creates chart data for the selected time range using the activity's daily aggregation mode.
    /// </summary>
    public TrendChartData GetChartData(
        ChartTimeRange chartTimeRange,
        int intervalsBack,
        TrendAggregation aggregation,
        List<NumericActivityDataEntry> numericEntries)
    {
        if (intervalsBack < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalsBack), "Interval offset cannot be negative.");
        }

        var now = DateTimeOffset.Now;
        var (rangeStart, rangeEnd) = GetTimeRange(chartTimeRange, intervalsBack, now);

        // keep all entries for the all-time view, otherwise limit them to the visible interval
        var entries = chartTimeRange == ChartTimeRange.All
            ? numericEntries
            : numericEntries
                .Where(e => e.Timestamp >= rangeStart && e.Timestamp < rangeEnd)
                .ToList();

        // choose how the filtered entries should be prepared for the selected chart range
        var dataPoints = chartTimeRange switch
        {
            ChartTimeRange.Day => GetDayData(entries, rangeStart, rangeEnd),
            ChartTimeRange.Week or ChartTimeRange.Month => GetDailyData(entries, aggregation),
            ChartTimeRange.Year => GetYearData(entries, aggregation),
            ChartTimeRange.All => GetAllData(entries, aggregation),
            _ => []
        };

        return new TrendChartData(DataPoints: dataPoints, RangeStart: rangeStart, RangeEnd: rangeEnd);
    }

    /// <summary>
    /// Calculates the visible start and end timestamps for a navigable chart interval.
    /// </summary>
    private static (DateTimeOffset? Start, DateTimeOffset? End) GetTimeRange(
        ChartTimeRange chartTimeRange,
        int intervalsBack,
        DateTimeOffset now)
    {
        // all-time view has no fixed interval boundaries
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

    /// <summary>
    /// Returns individual entries for the day view and visually downsamples them only when there are too many.
    /// </summary>
    private static List<TrendChartDataPoint> GetDayData(
        List<NumericActivityDataEntry> entries,
        DateTimeOffset? rangeStart,
        DateTimeOffset? rangeEnd)
    {
        // preserve individual measurements while the chart can display them comfortably
        if (entries.Count <= MaxDataPoints)
        {
            return entries
                .OrderBy(e => e.Timestamp)
                .Select(e => new TrendChartDataPoint(e.Timestamp, e.Value))
                .ToList();
        }

        // fall back to count-based downsampling if the visible interval is unavailable or invalid
        if (!rangeStart.HasValue || !rangeEnd.HasValue || rangeEnd <= rangeStart)
        {
            return DownsampleRawEntriesByCount(entries);
        }

        // excessive day-view entries are compressed only for visualization
        return DownsampleRawEntriesByTime(entries, rangeStart.Value, rangeEnd.Value);
    }

    /// <summary>
    /// Combines entries from the same local calendar day using the selected daily aggregation mode.
    /// </summary>
    private static List<TrendChartDataPoint> GetDailyData(
        List<NumericActivityDataEntry> entries,
        TrendAggregation aggregation)
    {
        // local dates define which entries belong to the same user-visible day
        return entries
            .GroupBy(e => e.Timestamp.ToLocalTime().Date)
            .Select(group => CreateDailyDataPoint(group, aggregation))
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Creates yearly chart data from daily values and compresses them into weekly averages when necessary.
    /// </summary>
    private static List<TrendChartDataPoint> GetYearData(
        List<NumericActivityDataEntry> entries,
        TrendAggregation aggregation)
    {
        // create the user-defined daily values before applying any visual compression
        var dailyData = GetDailyData(entries, aggregation);

        if (dailyData.Count <= MaxDataPoints)
        {
            return dailyData;
        }

        // average daily values by week so the selected daily aggregation keeps its original meaning
        return dailyData
            .GroupBy(e => GetStartOfWeek(e.Timestamp!.Value))
            .Select(CreateDownsampledDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Creates all-time chart data and progressively compresses daily values into larger time buckets when needed.
    /// </summary>
    private static List<TrendChartDataPoint> GetAllData(
        List<NumericActivityDataEntry> entries,
        TrendAggregation aggregation)
    {
        if (entries.Count == 0) return [];

        // always create semantic daily values before chart-level compression
        var dailyData = GetDailyData(entries, aggregation);

        if (dailyData.Count <= MaxDataPoints)
        {
            return dailyData;
        }

        // try weekly averages first
        var weeklyData = dailyData
            .GroupBy(e => GetStartOfWeek(e.Timestamp!.Value))
            .Select(CreateDownsampledDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (weeklyData.Count <= MaxDataPoints)
        {
            return weeklyData;
        }

        // use monthly averages when weekly data still produces too many points
        var monthlyData = dailyData
            .GroupBy(e =>
            {
                var local = e.Timestamp!.Value.ToLocalTime();
                return (local.Year, local.Month);
            })
            .Select(CreateDownsampledDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (monthlyData.Count <= MaxDataPoints)
        {
            return monthlyData;
        }

        // use yearly averages for very long histories
        var yearlyData = dailyData
            .GroupBy(e => e.Timestamp!.Value.ToLocalTime().Year)
            .Select(CreateDownsampledDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (yearlyData.Count <= MaxDataPoints)
        {
            return yearlyData;
        }

        // extremely long histories are compressed once more by point count
        return DownsampleDataPointsByCount(yearlyData);
    }

    /// <summary>
    /// Creates a single daily data point from same-day entries using the selected aggregation mode.
    /// </summary>
    private static TrendChartDataPoint CreateDailyDataPoint<TKey>(
        IGrouping<TKey, NumericActivityDataEntry> group,
        TrendAggregation aggregation)
    {
        // use the latest real entry as the representative timestamp for sum, average, and latest
        var latestEntry = group.MaxBy(e => e.Timestamp)!;

        return aggregation switch
        {
            TrendAggregation.Sum => new TrendChartDataPoint(
                latestEntry.Timestamp,
                group.Sum(e => e.Value)),

            TrendAggregation.Average => new TrendChartDataPoint(
                latestEntry.Timestamp,
                group.Average(e => e.Value)),

            TrendAggregation.Latest => new TrendChartDataPoint(
                latestEntry.Timestamp,
                latestEntry.Value),

            TrendAggregation.Maximum => CreateMaximumDailyDataPoint(group),

            _ => throw new ArgumentOutOfRangeException(
                nameof(aggregation),
                aggregation,
                "Unsupported trend aggregation type.")
        };
    }

    /// <summary>
    /// Creates a daily data point from the highest entry and preserves that entry's timestamp.
    /// </summary>
    private static TrendChartDataPoint CreateMaximumDailyDataPoint<TKey>(
        IGrouping<TKey, NumericActivityDataEntry> group)
    {
        // prefer the latest entry when multiple entries share the same maximum value
        var maximumEntry = group
            .OrderByDescending(e => e.Value)
            .ThenByDescending(e => e.Timestamp)
            .First();

        return new TrendChartDataPoint(
            maximumEntry.Timestamp,
            maximumEntry.Value);
    }

    /// <summary>
    /// Averages already calculated daily values into a larger chart bucket.
    /// </summary>
    private static TrendChartDataPoint CreateDownsampledDataPoint<TKey>(
        IGrouping<TKey, TrendChartDataPoint> group)
    {
        // use averaging only for visual compression, not as another daily aggregation step
        var latestPoint = group.MaxBy(e => e.Timestamp)!;
        var average = group.Average(e => e.Value);

        return latestPoint with { Value = average };
    }

    /// <summary>
    /// Downsamples raw day-view entries into evenly sized time buckets across the visible interval.
    /// </summary>
    private static List<TrendChartDataPoint> DownsampleRawEntriesByTime(
        List<NumericActivityDataEntry> entries,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd)
    {
        var startTicks = rangeStart.UtcDateTime.Ticks;
        var endTicks = rangeEnd.UtcDateTime.Ticks;
        var rangeTicks = endTicks - startTicks;

        // fall back to count-based compression if the interval cannot be divided safely
        if (rangeTicks <= 0)
        {
            return DownsampleRawEntriesByCount(entries);
        }

        // divide the entire visible interval into at most the preferred number of buckets
        var bucketSize = Math.Max(1L, (long)Math.Ceiling(rangeTicks / (double)MaxDataPoints));

        return entries
            .GroupBy(e =>
            {
                var offset = e.Timestamp.UtcDateTime.Ticks - startTicks;
                var bucket = offset / bucketSize;

                // keep edge-case timestamps inside the valid bucket range
                return Math.Clamp(bucket, 0, MaxDataPoints - 1);
            })
            .Select(CreateDownsampledRawDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Downsamples raw entries by splitting the ordered collection into similarly sized groups.
    /// </summary>
    private static List<TrendChartDataPoint> DownsampleRawEntriesByCount(
        List<NumericActivityDataEntry> entries)
    {
        // chronological ordering keeps neighboring measurements in the same buckets
        var orderedEntries = entries
            .OrderBy(e => e.Timestamp)
            .ToList();

        var bucketSize = Math.Max(1, (int)Math.Ceiling(orderedEntries.Count / (double)MaxDataPoints));

        var result = new List<TrendChartDataPoint>();

        // average each consecutive bucket while preserving its latest timestamp
        for (var i = 0; i < orderedEntries.Count; i += bucketSize)
        {
            var bucket = orderedEntries
                .Skip(i)
                .Take(bucketSize)
                .ToList();

            var latestEntry = bucket.MaxBy(e => e.Timestamp)!;
            var average = bucket.Average(e => e.Value);

            result.Add(new TrendChartDataPoint(latestEntry.Timestamp, average));
        }

        return result;
    }

    /// <summary>
    /// Creates a visually downsampled point by averaging raw entries inside a time bucket.
    /// </summary>
    private static TrendChartDataPoint CreateDownsampledRawDataPoint<TKey>(
        IGrouping<TKey, NumericActivityDataEntry> group)
    {
        // raw day-view entries are averaged only to reduce visual density
        var latestEntry = group.MaxBy(e => e.Timestamp)!;
        var average = group.Average(e => e.Value);

        return new TrendChartDataPoint(latestEntry.Timestamp, average);
    }

    /// <summary>
    /// Reduces an ordered collection of chart points to approximately the preferred maximum count.
    /// </summary>
    private static List<TrendChartDataPoint> DownsampleDataPointsByCount(
        List<TrendChartDataPoint> dataPoints)
    {
        // ignore points without timestamps because they cannot be placed chronologically
        var orderedDataPoints = dataPoints
            .Where(e => e.Timestamp.HasValue)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (orderedDataPoints.Count <= MaxDataPoints)
        {
            return orderedDataPoints;
        }

        var bucketSize = Math.Max(1, (int)Math.Ceiling(orderedDataPoints.Count / (double)MaxDataPoints));

        var result = new List<TrendChartDataPoint>();

        // average neighboring points while keeping the latest timestamp of each bucket
        for (var i = 0; i < orderedDataPoints.Count; i += bucketSize)
        {
            var bucket = orderedDataPoints
                .Skip(i)
                .Take(bucketSize)
                .ToList();

            var latestPoint = bucket.MaxBy(e => e.Timestamp)!;
            var average = bucket.Average(e => e.Value);

            result.Add(latestPoint with { Value = average });
        }

        return result;
    }

    /// <summary>
    /// Returns the local calendar date of the Monday that starts the given entry's week.
    /// </summary>
    private static DateTime GetStartOfWeek(DateTimeOffset date)
    {
        var localDate = date.ToLocalTime();

        // calculate how many days must be subtracted to reach Monday
        var diff = (7 + (localDate.DayOfWeek - DayOfWeek.Monday)) % 7;

        return localDate.AddDays(-diff).Date;
    }
}