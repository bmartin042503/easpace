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
    public List<TrendChartDataPoint> GetChartData(ChartTimeRange chartTimeRange, List<NumericActivityDataEntry> numericEntries)
    {
        if (numericEntries.Count == 0) return [];

        var now = DateTime.Now;

        const int maxDataPoints = 60;

        List<TrendChartDataPoint> dataPoints = [];

        switch (chartTimeRange)
        {
            case ChartTimeRange.Day:
                var dayData = numericEntries.Where(e => e.Timestamp >= now.AddDays(-1)).ToList();
                if (dayData.Count > maxDataPoints)
                {
                    // if there is too much data in a single day, we group by hour
                    dataPoints = dayData
                        .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day,
                            e.Timestamp.Hour, 0, 0))
                        .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else
                {
                    // day view: keep the raw, exact data, but map entity to DTO
                    dataPoints = dayData
                        .OrderBy(e => e.Timestamp)
                        .Select(e => new TrendChartDataPoint(e.Timestamp, e.Value))
                        .ToList();
                }

                break;

            case ChartTimeRange.Week:
                var weekData = numericEntries.Where(e => e.Timestamp >= now.AddDays(-7)).ToList();

                // week view: maximum 7 days., always group by day and average, no need for maxPoints check
                dataPoints = weekData
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                    .OrderBy(e => e.Timestamp).ToList();
                break;

            case ChartTimeRange.Month:
                var monthData = numericEntries.Where(e => e.Timestamp >= now.AddMonths(-1)).ToList();

                // month view: maximum 31 days, always group by day and average, no need for maxPoints check
                dataPoints = monthData
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                    .OrderBy(e => e.Timestamp).ToList();
                break;

            case ChartTimeRange.Year:
                var yearData = numericEntries.Where(e => e.Timestamp >= now.AddYears(-1)).ToList();

                // year view: always get the daily average first to avoid same-day duplications
                var dailyYearData = yearData
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                    .ToList();

                if (dailyYearData.Count > maxDataPoints)
                {
                    // if we still have more than 60 points daily, we group by week
                    dataPoints = yearData
                        .GroupBy(e => GetStartOfWeek(e.Timestamp))
                        .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                        .OrderBy(e => e.Timestamp).ToList();
                }
                else
                {
                    // if it fits within the 60 points limit, we show the daily averaged data (not the raw)
                    dataPoints = dailyYearData.OrderBy(e => e.Timestamp).ToList();
                }

                break;

            case ChartTimeRange.All:
                var allData = numericEntries.ToList();

                // all view: always get the daily average first
                var dailyAllData = allData
                    .GroupBy(e => e.Timestamp.Date)
                    .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                    .ToList();

                if (dailyAllData.Count > maxDataPoints)
                {
                    var totalDays = (allData.Max(e => e.Timestamp) - allData.Min(e => e.Timestamp)).TotalDays;

                    if (totalDays > 730)
                    {
                        // > 2 years -> group by month
                        dataPoints = allData
                            .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, 1))
                            .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                            .OrderBy(e => e.Timestamp).ToList();
                    }
                    else
                    {
                        // < 2 years -> group by week
                        dataPoints = allData
                            .GroupBy(e => GetStartOfWeek(e.Timestamp))
                            .Select(g => new TrendChartDataPoint(g.Key, Math.Round(g.Average(x => x.Value), 1)))
                            .OrderBy(e => e.Timestamp).ToList();
                    }
                }
                else
                {
                    // if it fits within the 60 points limit, we show the daily averaged data
                    dataPoints = dailyAllData.OrderBy(e => e.Timestamp).ToList();
                }

                break;
        }

        return dataPoints;
    }

    private static DateTime GetStartOfWeek(DateTimeOffset dto)
    {
        var diff = (7 + (dto.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dto.AddDays(-1 * diff).Date;
    }
}