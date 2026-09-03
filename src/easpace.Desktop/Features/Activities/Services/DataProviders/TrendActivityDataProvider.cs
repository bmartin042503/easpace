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
    
    private readonly TimeProvider _timeProvider;

    public TrendActivityDataProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates chart data for the selected time range using the activity's daily aggregation mode.
    /// </summary>
    public TrendChartData GetChartData(
        ChartTimeRange chartTimeRange,
        int intervalsBack,
        TrendAggregation aggregation,
        List<NumericActivityDataEntry> numericEntries)
    {
        if (intervalsBack < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalsBack), "Interval offset cannot be less than -1.");
        }

        var now = _timeProvider.GetLocalNow();
        var (rangeStart, rangeEnd) = GetTimeRange(chartTimeRange, intervalsBack, now);

        // keep all entries for the all-time view, otherwise limit them to the visible interval
        var entries = chartTimeRange == ChartTimeRange.All
            ? numericEntries
            : numericEntries
                .Where(e => e.Timestamp >= rangeStart && e.Timestamp < rangeEnd)
                .ToList();

        // prepare entries according to the semantic meaning of the selected chart range
        var dataPoints = chartTimeRange switch
        {
            ChartTimeRange.Day => GetDayData(entries, rangeStart, rangeEnd),
            ChartTimeRange.Week or ChartTimeRange.Month => GetDailyData(entries, aggregation),
            ChartTimeRange.Year => GetYearData(entries, aggregation),
            ChartTimeRange.All => GetAllData(entries, aggregation),
            _ => []
        };

        return new TrendChartData(
            DataPoints: dataPoints,
            RangeStart: rangeStart,
            RangeEnd: rangeEnd);
    }
    
    /// <summary>
    /// Calculates the aggregated value for the local calendar day containing the specified date.
    /// </summary>
    public double? GetDailyValue(
        DateTimeOffset date, 
        TrendAggregation aggregation, 
        IEnumerable<NumericActivityDataEntry> numericEntries)
    {
        var localDate = date.ToLocalTime().Date;

        var entries = numericEntries
            .Where(e => e.Timestamp.ToLocalTime().Date == localDate)
            .ToList();

        if (entries.Count == 0)
        {
            return null;
        }

        return aggregation switch
        {
            TrendAggregation.Sum => entries.Sum(e => e.Value),
            TrendAggregation.Average => entries.Average(e => e.Value),
            TrendAggregation.Latest => entries.MaxBy(e => e.Timestamp)!.Value,
            TrendAggregation.Maximum => entries.Max(e => e.Value),

            _ => throw new ArgumentOutOfRangeException(
                nameof(aggregation),
                aggregation,
                "Unsupported trend aggregation type.")
        };
    }

    /// <summary>
    /// Calculates calendar-aligned start and exclusive end timestamps for a navigable chart interval.
    /// </summary>
    private static (DateTimeOffset? Start, DateTimeOffset? End) GetTimeRange(
        ChartTimeRange chartTimeRange,
        int intervalsBack,
        DateTimeOffset now)
    {
        // calculate all boundaries using local calendar dates
        var localNow = now.ToLocalTime();
        var currentDayStart = localNow.Date;
        var currentWeekStart = GetStartOfWeek(currentDayStart);
        var currentMonthStart = new DateTime(localNow.Year, localNow.Month, 1);
        var currentYearStart = new DateTime(localNow.Year, 1, 1);

        return chartTimeRange switch
        {
            ChartTimeRange.Day => CreateLocalRange(
                currentDayStart.AddDays(-intervalsBack),
                currentDayStart.AddDays(1 - intervalsBack)),

            ChartTimeRange.Week => CreateLocalRange(
                currentWeekStart.AddDays(-7 * intervalsBack),
                currentWeekStart.AddDays(7 * (1 - intervalsBack))),

            ChartTimeRange.Month => CreateLocalRange(
                currentMonthStart.AddMonths(-intervalsBack),
                currentMonthStart.AddMonths(1 - intervalsBack)),

            ChartTimeRange.Year => CreateLocalRange(
                currentYearStart.AddYears(-intervalsBack),
                currentYearStart.AddYears(1 - intervalsBack)),

            _ => (null, null)
        };
    }

    /// <summary>
    /// Converts local calendar boundaries to DateTimeOffset values using the correct local UTC offset.
    /// </summary>
    private static (DateTimeOffset Start, DateTimeOffset End) CreateLocalRange(DateTime start, DateTime end)
    {
        return (CreateLocalDateTimeOffset(start), CreateLocalDateTimeOffset(end));
    }

    /// <summary>
    /// Creates a DateTimeOffset from a local date and applies the local time zone offset.
    /// </summary>
    private static DateTimeOffset CreateLocalDateTimeOffset(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var offset = TimeZoneInfo.Local.GetUtcOffset(unspecified);

        return new DateTimeOffset(unspecified, offset);
    }

    /// <summary>
    /// Returns the visual midpoint between two timestamps.
    /// </summary>
    private static DateTimeOffset GetMidpoint(DateTimeOffset start, DateTimeOffset end)
    {
        var startTicks = start.UtcDateTime.Ticks;
        var endTicks = end.UtcDateTime.Ticks;
        var midpointTicks = startTicks + (endTicks - startTicks) / 2;

        return new DateTimeOffset(midpointTicks, TimeSpan.Zero).ToLocalTime();
    }

    /// <summary>
    /// Returns the midpoint of a local calendar period.
    /// </summary>
    private static DateTimeOffset GetLocalPeriodMidpoint(DateTime start, DateTime end)
    {
        var (rangeStart, rangeEnd) = CreateLocalRange(start, end);
        return GetMidpoint(rangeStart, rangeEnd);
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

        // average semantic daily values into calendar weeks
        return dailyData
            .GroupBy(e => GetStartOfWeek(e.Timestamp!.Value))
            .Select(CreateWeeklyDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Creates all-time chart data and progressively compresses daily values into larger calendar periods when needed.
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
            .Select(CreateWeeklyDataPoint)
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
            .Select(CreateMonthlyDataPoint)
            .OrderBy(e => e.Timestamp)
            .ToList();

        if (monthlyData.Count <= MaxDataPoints)
        {
            return monthlyData;
        }

        // use yearly averages for very long histories
        var yearlyData = dailyData
            .GroupBy(e => e.Timestamp!.Value.ToLocalTime().Year)
            .Select(CreateYearlyDataPoint)
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
    private static TrendChartDataPoint CreateDailyDataPoint(
        IGrouping<DateTime, NumericActivityDataEntry> group,
        TrendAggregation aggregation)
    {
        // place the daily value at the visual center of its local calendar day
        var timestamp = GetLocalPeriodMidpoint(
            group.Key,
            group.Key.AddDays(1));

        var value = aggregation switch
        {
            TrendAggregation.Sum => group.Sum(e => e.Value),
            TrendAggregation.Average => group.Average(e => e.Value),
            TrendAggregation.Latest => group.MaxBy(e => e.Timestamp)!.Value,
            TrendAggregation.Maximum => group.Max(e => e.Value),

            _ => throw new ArgumentOutOfRangeException(
                nameof(aggregation),
                aggregation,
                "Unsupported trend aggregation type.")
        };

        return new TrendChartDataPoint(timestamp, value);
    }

    /// <summary>
    /// Creates a weekly chart point by averaging daily values and placing the result at the center of the week.
    /// </summary>
    private static TrendChartDataPoint CreateWeeklyDataPoint(IGrouping<DateTime, TrendChartDataPoint> group)
    {
        // the key is the Monday that starts the represented local calendar week
        var timestamp = GetLocalPeriodMidpoint(
            group.Key,
            group.Key.AddDays(7));

        return new TrendChartDataPoint(
            timestamp,
            group.Average(e => e.Value));
    }

    /// <summary>
    /// Creates a monthly chart point by averaging daily values and placing the result at the center of the month.
    /// </summary>
    private static TrendChartDataPoint CreateMonthlyDataPoint(
        IGrouping<(int Year, int Month), TrendChartDataPoint> group)
    {
        var start = new DateTime(group.Key.Year, group.Key.Month, 1);
        var end = start.AddMonths(1);

        // place the compressed value at the visual center of the represented month
        return new TrendChartDataPoint(
            GetLocalPeriodMidpoint(start, end),
            group.Average(e => e.Value));
    }

    /// <summary>
    /// Creates a yearly chart point by averaging daily values and placing the result at the center of the year.
    /// </summary>
    private static TrendChartDataPoint CreateYearlyDataPoint(IGrouping<int, TrendChartDataPoint> group)
    {
        var start = new DateTime(group.Key, 1, 1);
        var end = start.AddYears(1);

        // place the compressed value at the visual center of the represented year
        return new TrendChartDataPoint(
            GetLocalPeriodMidpoint(start, end),
            group.Average(e => e.Value));
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
        var bucketSize = Math.Max(
            1L,
            (long)Math.Ceiling(rangeTicks / (double)MaxDataPoints));

        return entries
            .GroupBy(e =>
            {
                var offset = e.Timestamp.UtcDateTime.Ticks - startTicks;
                return Math.Clamp(offset / bucketSize, 0, MaxDataPoints - 1);
            })
            .Select(group => CreateDownsampledRawDataPoint(
                group,
                rangeStart,
                rangeEnd,
                bucketSize))
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Creates a visually downsampled point at the center of its time bucket.
    /// </summary>
    private static TrendChartDataPoint CreateDownsampledRawDataPoint(
        IGrouping<long, NumericActivityDataEntry> group,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        long bucketSize)
    {
        var rangeStartTicks = rangeStart.UtcDateTime.Ticks;
        var rangeEndTicks = rangeEnd.UtcDateTime.Ticks;

        var bucketStartTicks = rangeStartTicks + group.Key * bucketSize;
        var bucketEndTicks = Math.Min(bucketStartTicks + bucketSize, rangeEndTicks);

        var bucketStart = new DateTimeOffset(bucketStartTicks, TimeSpan.Zero);
        var bucketEnd = new DateTimeOffset(bucketEndTicks, TimeSpan.Zero);

        // the selected daily aggregation does not affect visual day-view compression
        return new TrendChartDataPoint(
            GetMidpoint(bucketStart, bucketEnd),
            group.Average(e => e.Value));
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

        // average consecutive entries and place each result at the center of its represented timestamps
        for (var i = 0; i < orderedEntries.Count; i += bucketSize)
        {
            var bucket = orderedEntries
                .Skip(i)
                .Take(bucketSize)
                .ToList();

            var firstEntry = bucket[0];
            var lastEntry = bucket[^1];

            var timestamp = bucket.Count == 1
                ? firstEntry.Timestamp
                : GetMidpoint(firstEntry.Timestamp, lastEntry.Timestamp);

            result.Add(new TrendChartDataPoint(
                timestamp,
                bucket.Average(e => e.Value)));
        }

        return result;
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

        var bucketSize = Math.Max(
            1,
            (int)Math.Ceiling(orderedDataPoints.Count / (double)MaxDataPoints));

        var result = new List<TrendChartDataPoint>();

        // average neighboring points and position the result at their temporal center
        for (var i = 0; i < orderedDataPoints.Count; i += bucketSize)
        {
            var bucket = orderedDataPoints
                .Skip(i)
                .Take(bucketSize)
                .ToList();

            var firstTimestamp = bucket[0].Timestamp!.Value;
            var lastTimestamp = bucket[^1].Timestamp!.Value;

            var timestamp = bucket.Count == 1
                ? firstTimestamp
                : GetMidpoint(firstTimestamp, lastTimestamp);

            result.Add(new TrendChartDataPoint(
                timestamp,
                bucket.Average(e => e.Value)));
        }

        return result;
    }

    /// <summary>
    /// Returns the local calendar date of the Monday that starts the given entry's week.
    /// </summary>
    private static DateTime GetStartOfWeek(DateTimeOffset date)
    {
        return GetStartOfWeek(date.ToLocalTime().Date);
    }

    /// <summary>
    /// Returns the Monday that starts the week containing the given local calendar date.
    /// </summary>
    private static DateTime GetStartOfWeek(DateTime date)
    {
        // calculate how many days must be subtracted to reach Monday
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;

        return date.AddDays(-diff).Date;
    }
}