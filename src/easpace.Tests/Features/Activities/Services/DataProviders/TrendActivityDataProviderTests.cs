// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services.DataProviders;
using FluentAssertions;

namespace easpace.Tests.Features.Activities.Services.DataProviders;

public class TrendActivityDataProviderTests
{
    #region Validation

    [Fact]
    public void GetChartData_ShouldThrow_WhenIntervalsBackIsLessThanMinusOne()
    {
        var sut = CreateSut();

        var action = () => sut.GetChartData(ChartTimeRange.Day, -2, TrendAggregation.Average, []);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("intervalsBack");
    }

    [Fact]
    public void GetChartData_ShouldAllowMinusOneInterval()
    {
        var sut = CreateSut(Local(2026, 9, 3, 12));

        var action = () => sut.GetChartData(ChartTimeRange.Day, -1, TrendAggregation.Average, []);

        action.Should().NotThrow();
    }

    [Fact]
    public void GetChartData_ShouldThrow_WhenAggregationIsUnsupportedAndAggregationIsRequired()
    {
        var sut = CreateSut(Local(2026, 9, 3, 12));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 3, 8), 10)
        };

        var action = () => sut.GetChartData(ChartTimeRange.Week, 0, (TrendAggregation)999, entries);

        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("aggregation");
    }

    #endregion

    #region Day ranges

    [Fact]
    public void GetChartData_Day_ShouldUseCurrentCalendarDay()
    {
        var sut = CreateSut(Local(2026, 9, 3, 14, 37));

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 9, 3));
        result.RangeEnd.Should().Be(Local(2026, 9, 4));
    }

    [Fact]
    public void GetChartData_Day_ShouldUsePreviousCalendarDay()
    {
        var sut = CreateSut(Local(2026, 9, 3, 14, 37));

        var result = sut.GetChartData(ChartTimeRange.Day, 1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 9, 2));
        result.RangeEnd.Should().Be(Local(2026, 9, 3));
    }

    [Fact]
    public void GetChartData_Day_ShouldUseNextCalendarDay_WhenIntervalsBackIsMinusOne()
    {
        var sut = CreateSut(Local(2026, 9, 3, 14, 37));

        var result = sut.GetChartData(ChartTimeRange.Day, -1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 9, 4));
        result.RangeEnd.Should().Be(Local(2026, 9, 5));
    }

    [Fact]
    public void GetChartData_Day_ShouldIncludeRangeStartAndExcludeRangeEnd()
    {
        var sut = CreateSut(Local(2026, 9, 3, 14));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 23, 59, 59), 1),
            Entry(Local(2026, 9, 3), 2),
            Entry(Local(2026, 9, 3, 12), 3),
            Entry(Local(2026, 9, 3, 23, 59, 59), 4),
            Entry(Local(2026, 9, 4), 5)
        };

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Select(e => e.Value).Should().Equal(2, 3, 4);
    }

    [Fact]
    public void GetChartData_Day_ShouldReturnRawEntries()
    {
        var sut = CreateSut(Local(2026, 9, 3, 20));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 3, 8), 5),
            Entry(Local(2026, 9, 3, 12), 10),
            Entry(Local(2026, 9, 3, 18), 15)
        };

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(3);

        result.DataPoints.Select(e => e.Value).Should().Equal(5, 10, 15);
    }

    [Fact]
    public void GetChartData_Day_ShouldReturnEntriesChronologically()
    {
        var sut = CreateSut(Local(2026, 9, 3, 20));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 3, 18), 3),
            Entry(Local(2026, 9, 3, 8), 1),
            Entry(Local(2026, 9, 3, 12), 2)
        };

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Average, entries);

        result.DataPoints.Select(e => e.Value).Should().Equal(1, 2, 3);

        result.DataPoints.Select(e => e.Timestamp!.Value).Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetChartData_Day_ShouldIgnoreDailyAggregationSetting()
    {
        var now = Local(2026, 9, 3, 20);

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 3, 8), 2),
            Entry(Local(2026, 9, 3, 12), 10),
            Entry(Local(2026, 9, 3, 18), 4)
        };

        var sum = CreateSut(now).GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Sum, entries);
        var average = CreateSut(now).GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Average, entries);
        var latest = CreateSut(now).GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Latest, entries);
        var maximum = CreateSut(now).GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Maximum, entries);

        average.DataPoints.Should().Equal(sum.DataPoints);
        latest.DataPoints.Should().Equal(sum.DataPoints);
        maximum.DataPoints.Should().Equal(sum.DataPoints);
    }

    #endregion

    #region Week ranges

    [Fact]
    public void GetChartData_Week_ShouldStartOnMonday()
    {
        // 2026-09-03 is Thursday
        var sut = CreateSut(Local(2026, 9, 3, 12));

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 8, 31));
        result.RangeEnd.Should().Be(Local(2026, 9, 7));
    }

    [Fact]
    public void GetChartData_Week_ShouldNavigateToPreviousCalendarWeek()
    {
        var sut = CreateSut(Local(2026, 9, 3, 12));

        var result = sut.GetChartData(ChartTimeRange.Week, 1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 8, 24));
        result.RangeEnd.Should().Be(Local(2026, 8, 31));
    }

    [Fact]
    public void GetChartData_Week_ShouldNavigateToNextCalendarWeek()
    {
        var sut = CreateSut(Local(2026, 9, 3, 12));

        var result = sut.GetChartData(ChartTimeRange.Week, -1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 9, 7));
        result.RangeEnd.Should().Be(Local(2026, 9, 14));
    }

    [Fact]
    public void GetChartData_Week_ShouldHandleYearBoundary()
    {
        // 2026-01-01 is Thursday
        var sut = CreateSut(Local(2026, 1, 1, 12));

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2025, 12, 29));
        result.RangeEnd.Should().Be(Local(2026, 1, 5));
    }

    #endregion

    #region Month ranges

    [Fact]
    public void GetChartData_Month_ShouldUseCurrentCalendarMonth()
    {
        var sut = CreateSut(Local(2026, 9, 17, 14));

        var result = sut.GetChartData(ChartTimeRange.Month, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 9, 1));
        result.RangeEnd.Should().Be(Local(2026, 10, 1));
    }

    [Fact]
    public void GetChartData_Month_ShouldNavigateToPreviousCalendarMonth()
    {
        var sut = CreateSut(Local(2026, 9, 17));

        var result = sut.GetChartData(ChartTimeRange.Month, 1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 8, 1));
        result.RangeEnd.Should().Be(Local(2026, 9, 1));
    }

    [Fact]
    public void GetChartData_Month_ShouldNavigateToNextCalendarMonth()
    {
        var sut = CreateSut(Local(2026, 9, 17));

        var result = sut.GetChartData(ChartTimeRange.Month, -1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 10, 1));
        result.RangeEnd.Should().Be(Local(2026, 11, 1));
    }

    [Fact]
    public void GetChartData_Month_ShouldHandleYearBoundary()
    {
        var sut = CreateSut(Local(2026, 1, 15));

        var result = sut.GetChartData(ChartTimeRange.Month, 1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2025, 12, 1));
        result.RangeEnd.Should().Be(Local(2026, 1, 1));
    }

    [Fact]
    public void GetChartData_Month_ShouldHandleLeapYearFebruary()
    {
        var sut = CreateSut(Local(2024, 2, 15));

        var result = sut.GetChartData(ChartTimeRange.Month, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2024, 2, 1));
        result.RangeEnd.Should().Be(Local(2024, 3, 1));
    }

    #endregion

    #region Year ranges

    [Fact]
    public void GetChartData_Year_ShouldUseCurrentCalendarYear()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var result = sut.GetChartData(ChartTimeRange.Year, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2026, 1, 1));
        result.RangeEnd.Should().Be(Local(2027, 1, 1));
    }

    [Fact]
    public void GetChartData_Year_ShouldNavigateToPreviousCalendarYear()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var result = sut.GetChartData(ChartTimeRange.Year, 1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2025, 1, 1));
        result.RangeEnd.Should().Be(Local(2026, 1, 1));
    }

    [Fact]
    public void GetChartData_Year_ShouldNavigateToNextCalendarYear()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var result = sut.GetChartData(ChartTimeRange.Year, -1, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2027, 1, 1));
        result.RangeEnd.Should().Be(Local(2028, 1, 1));
    }

    [Fact]
    public void GetChartData_Year_ShouldHandleLeapYear()
    {
        var sut = CreateSut(Local(2024, 6, 1));

        var result = sut.GetChartData(ChartTimeRange.Year, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().Be(Local(2024, 1, 1));
        result.RangeEnd.Should().Be(Local(2025, 1, 1));
    }

    #endregion

    #region Daily aggregation

    [Fact]
    public void GetChartData_Sum_ShouldSumEntriesFromSameDay()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = EntriesForSameDay([1.0, 2.0, 3.0, 4.0]);

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().ContainSingle();
        result.DataPoints.Single().Value.Should().Be(10);
    }

    [Fact]
    public void GetChartData_Average_ShouldAverageEntriesFromSameDay()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = EntriesForSameDay([1.0, 2.0, 3.0, 4.0]);

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Average, entries);

        result.DataPoints.Should().ContainSingle();
        result.DataPoints.Single().Value.Should().Be(2.5);
    }

    [Fact]
    public void GetChartData_Average_ShouldNotRoundCalculatedValue()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = EntriesForSameDay([1.0, 2.0, 2.0]);

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Average, entries);

        result.DataPoints.Single().Value.Should().BeApproximately(5.0 / 3.0, 0.000000000001);
    }

    [Fact]
    public void GetChartData_Latest_ShouldUseChronologicallyLatestEntry()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 18), 30),
            Entry(Local(2026, 9, 2, 8), 10),
            Entry(Local(2026, 9, 2, 12), 20)
        };

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Latest, entries);

        result.DataPoints.Single().Value.Should().Be(30);
    }

    [Fact]
    public void GetChartData_Maximum_ShouldUseHighestValue()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 8), 10),
            Entry(Local(2026, 9, 2, 12), 100),
            Entry(Local(2026, 9, 2, 18), 20)
        };

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Maximum, entries);

        result.DataPoints.Single().Value.Should().Be(100);
    }

    [Fact]
    public void GetChartData_Maximum_ShouldHandleOnlyNegativeValues()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 8), -20),
            Entry(Local(2026, 9, 2, 12), -5),
            Entry(Local(2026, 9, 2, 18), -10)
        };

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Maximum, entries);

        result.DataPoints.Single().Value.Should().Be(-5);
    }

    [Theory]
    [InlineData(TrendAggregation.Sum)]
    [InlineData(TrendAggregation.Average)]
    [InlineData(TrendAggregation.Latest)]
    [InlineData(TrendAggregation.Maximum)]
    public void GetChartData_DailyAggregation_ShouldPreserveSingleValue(TrendAggregation aggregation)
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 18), 42.5)
        };

        var result = sut.GetChartData(ChartTimeRange.Week, 0, aggregation, entries);

        result.DataPoints.Single().Value.Should().Be(42.5);
    }

    [Fact]
    public void GetChartData_DailyAggregation_ShouldCreateSeparatePointForEachDay()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 8, 31, 8), 1),
            Entry(Local(2026, 8, 31, 18), 2),

            Entry(Local(2026, 9, 1, 8), 3),
            Entry(Local(2026, 9, 1, 18), 4),

            Entry(Local(2026, 9, 2, 8), 5)
        };

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(3);

        result.DataPoints.Select(e => e.Value).Should().Equal(3, 7, 5);
    }

    [Fact]
    public void GetChartData_DailyAggregation_ShouldPlacePointAtCenterOfCalendarDay()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 8), 5),
            Entry(Local(2026, 9, 2, 22), 10)
        };

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Sum, entries);

        var expectedTimestamp = LocalPeriodMidpoint(
            new DateTime(2026, 9, 2),
            new DateTime(2026, 9, 3));

        result.DataPoints.Single().Timestamp.Should().Be(expectedTimestamp);
    }

    [Fact]
    public void GetChartData_DailyAggregation_TimestampShouldNotDependOnEntryTimes()
    {
        var now = Local(2026, 9, 3);

        var morningEntries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 1), 5),
            Entry(Local(2026, 9, 2, 2), 10)
        };

        var eveningEntries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 21), 5),
            Entry(Local(2026, 9, 2, 23), 10)
        };

        var morningResult = CreateSut(now).GetChartData(
            ChartTimeRange.Week,
            0,
            TrendAggregation.Sum,
            morningEntries);

        var eveningResult = CreateSut(now).GetChartData(
            ChartTimeRange.Week,
            0,
            TrendAggregation.Sum,
            eveningEntries);

        morningResult.DataPoints.Single().Timestamp.Should().Be(eveningResult.DataPoints.Single().Timestamp);
    }

    #endregion

    #region Week and month daily representation

    [Fact]
    public void GetChartData_Week_ShouldUseDailyAggregatedValues()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 8, 31, 8), 2),
            Entry(Local(2026, 8, 31, 18), 3),

            Entry(Local(2026, 9, 1, 8), 10),
            Entry(Local(2026, 9, 1, 18), 20)
        };

        var result = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Select(e => e.Value).Should().Equal(5, 30);
    }

    [Fact]
    public void GetChartData_Month_ShouldUseDailyAggregatedValues()
    {
        var sut = CreateSut(Local(2026, 9, 15));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 1, 8), 2),
            Entry(Local(2026, 9, 1, 18), 3),

            Entry(Local(2026, 9, 20, 8), 10),
            Entry(Local(2026, 9, 20, 18), 20)
        };

        var result = sut.GetChartData(ChartTimeRange.Month, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Select(e => e.Value).Should().Equal(5, 30);
    }

    [Fact]
    public void GetChartData_Month_ShouldNotDownsampleBasedOnRawEntryCount()
    {
        var sut = CreateSut(Local(2026, 9, 15));

        var entries = Enumerable.Range(0, 100)
            .Select(i => Entry(Local(2026, 9, 10, 12).AddSeconds(i), 1))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Month, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().ContainSingle();
        result.DataPoints.Single().Value.Should().Be(100);
    }

    #endregion

    #region Day downsampling

    [Fact]
    public void GetChartData_Day_ShouldNotDownsampleExactlySixtyEntries()
    {
        var sut = CreateSut(Local(2026, 9, 3, 20));

        var entries = Enumerable.Range(0, 60)
            .Select(i => Entry(Local(2026, 9, 3, 8).AddSeconds(i), i))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Average, entries);

        result.DataPoints.Should().HaveCount(60);

        result.DataPoints.Select(e => e.Value).Should().Equal(Enumerable.Range(0, 60).Select(i => (double)i));
    }

    [Fact]
    public void GetChartData_Day_ShouldDownsampleMoreThanSixtyEntries()
    {
        var sut = CreateSut(Local(2026, 9, 3, 20));

        var entries = Enumerable.Range(0, 240)
            .Select(i => Entry(Local(2026, 9, 3).AddMinutes(i * 5), i))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Average, entries);

        result.DataPoints.Should().NotBeEmpty();
        result.DataPoints.Should().HaveCountLessThanOrEqualTo(60);

        result.DataPoints.Select(e => e.Timestamp!.Value).Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetChartData_Day_Downsampling_ShouldAverageValuesInsideTimeBucket()
    {
        var now = Local(2026, 9, 3, 12);
        var sut = CreateSut(now);

        var rangeStart = Local(2026, 9, 3);
        var rangeEnd = Local(2026, 9, 4);
        var rangeTicks = rangeEnd.UtcDateTime.Ticks - rangeStart.UtcDateTime.Ticks;
        
        var bucketSize = (long)Math.Ceiling(rangeTicks / 60.0);
        var firstBucketTimestamp = FromUtcTicks(rangeStart.UtcDateTime.Ticks + 1);
        var secondBucketTimestamp = FromUtcTicks(rangeStart.UtcDateTime.Ticks + bucketSize + 1);
        
        var entries = Enumerable.Range(0, 60).Select(_ => Entry(firstBucketTimestamp, 2)).ToList();
        entries.Add(Entry(secondBucketTimestamp, 10));

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Maximum, entries);

        result.DataPoints.Should().HaveCount(2);
        result.DataPoints[0].Value.Should().Be(2);
        result.DataPoints[1].Value.Should().Be(10);
    }

    [Fact]
    public void GetChartData_Day_Downsampling_ShouldPlacePointAtCenterOfTimeBucket()
    {
        var now = Local(2026, 9, 3, 12);
        var sut = CreateSut(now);

        var rangeStart = Local(2026, 9, 3);
        var rangeEnd = Local(2026, 9, 4);
        var rangeTicks = rangeEnd.UtcDateTime.Ticks - rangeStart.UtcDateTime.Ticks;

        var bucketSize = (long)Math.Ceiling(rangeTicks / 60.0);
        var firstBucketTimestamp = FromUtcTicks(rangeStart.UtcDateTime.Ticks + 1);
        var secondBucketTimestamp = FromUtcTicks(rangeStart.UtcDateTime.Ticks + bucketSize + 1);

        var entries = Enumerable.Range(0, 60).Select(_ => Entry(firstBucketTimestamp, 1)).ToList();
        entries.Add(Entry(secondBucketTimestamp, 1));

        var result = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Average, entries);

        var expectedFirstMidpoint = Midpoint(rangeStart, FromUtcTicks(rangeStart.UtcDateTime.Ticks + bucketSize));

        result.DataPoints[0].Timestamp.Should().Be(expectedFirstMidpoint);
    }

    [Fact]
    public void GetChartData_Day_Downsampling_ShouldIgnoreDailyAggregationSetting()
    {
        var now = Local(2026, 9, 3, 20);

        var entries = Enumerable.Range(0, 120)
            .Select(i => Entry(Local(2026, 9, 3).AddMinutes(i * 5), i + 1))
            .ToList();

        var sum = CreateSut(now).GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Sum, entries);
        var maximum = CreateSut(now).GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Maximum, entries);

        maximum.DataPoints.Should().Equal(sum.DataPoints);
    }

    #endregion

    #region Year downsampling

    [Fact]
    public void GetChartData_Year_ShouldKeepDailyValues_WhenThereAreExactlySixtyDays()
    {
        var sut = CreateSut(Local(2026, 12, 31));

        var start = new DateTime(2026, 1, 5);

        var entries = Enumerable.Range(0, 60)
            .Select(i => Entry(Local(start.AddDays(i).AddHours(12)), i))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Year, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(60);

        result.DataPoints[0].Timestamp.Should().Be(LocalPeriodMidpoint(start, start.AddDays(1)));
    }

    [Fact]
    public void GetChartData_Year_ShouldCompressToWeeks_WhenThereAreMoreThanSixtyDailyValues()
    {
        var sut = CreateSut(Local(2026, 12, 31));

        // 2026-01-05 is Monday, so this creates ten complete weeks
        var start = new DateTime(2026, 1, 5);

        var entries = Enumerable.Range(0, 70)
            .Select(i => Entry(Local(start.AddDays(i).AddHours(12)), i % 7 + 1))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Year, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(10);

        result.DataPoints.Should().OnlyContain(e => Math.Abs(e.Value - 4) < 0.000000001);
    }

    [Fact]
    public void GetChartData_Year_WeeklyCompression_ShouldPlacePointAtCenterOfWeek()
    {
        var sut = CreateSut(Local(2026, 12, 31));

        var start = new DateTime(2026, 1, 5);

        var entries = Enumerable.Range(0, 70)
            .Select(i => Entry(Local(start.AddDays(i).AddHours(12)), 10))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Year, 0, TrendAggregation.Sum, entries);

        var expectedFirstWeekTimestamp = LocalPeriodMidpoint(start, start.AddDays(7));

        result.DataPoints[0].Timestamp.Should().Be(expectedFirstWeekTimestamp);
    }

    [Fact]
    public void GetChartData_Year_ShouldApplyDailyAggregationBeforeWeeklyCompression()
    {
        var sut = CreateSut(Local(2026, 12, 31));

        var start = new DateTime(2026, 1, 5);

        var entries = Enumerable.Range(0, 70)
            .SelectMany(i =>
            {
                var day = start.AddDays(i);

                return new[]
                {
                    Entry(Local(day.AddHours(8)), 2),
                    Entry(Local(day.AddHours(18)), 4)
                };
            })
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Year, 0, TrendAggregation.Sum, entries);

        // every daily semantic value is 6,
        // therefore weekly visual averaging must also remain 6
        result.DataPoints.Should().OnlyContain(e => Math.Abs(e.Value - 6) < 0.000000001);
    }

    [Fact]
    public void GetChartData_Year_WeeklyCompression_ShouldIgnoreMissingDaysInsteadOfTreatingThemAsZero()
    {
        var sut = CreateSut(Local(2026, 12, 31));

        var start = new DateTime(2026, 1, 5);

        // eleven weeks with one missing day per week = 66 daily points,
        // which is still enough to trigger weekly compression
        var entries = Enumerable.Range(0, 77)
            .Where(i => i % 7 != 2)
            .Select(i => Entry(Local(start.AddDays(i).AddHours(12)), 10))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.Year, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().OnlyContain(e => Math.Abs(e.Value - 10) < 0.000000001);
    }

    #endregion

    #region All range

    [Fact]
    public void GetChartData_All_ShouldHaveNoFixedRange()
    {
        var sut = CreateSut();

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Average, []);

        result.RangeStart.Should().BeNull();
        result.RangeEnd.Should().BeNull();
    }

    [Fact]
    public void GetChartData_All_ShouldReturnEmptyData_WhenThereAreNoEntries()
    {
        var sut = CreateSut();

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Average, []);

        result.DataPoints.Should().BeEmpty();
    }

    [Fact]
    public void GetChartData_All_ShouldNotFilterOldOrFutureEntries()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(1990, 1, 1, 12), 1),
            Entry(Local(2026, 9, 3, 12), 2),
            Entry(Local(2050, 1, 1, 12), 3)
        };

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(3);
    }

    [Fact]
    public void GetChartData_All_ShouldAggregateEntriesFromSameDay()
    {
        var sut = CreateSut();

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2020, 1, 1, 8), 2),
            Entry(Local(2020, 1, 1, 18), 3)
        };

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().ContainSingle();
        result.DataPoints.Single().Value.Should().Be(5);
    }

    [Fact]
    public void GetChartData_All_ShouldKeepDailyData_WhenThereAreExactlySixtyDays()
    {
        var sut = CreateSut();

        var start = new DateTime(2026, 1, 1);

        var entries = Enumerable.Range(0, 60)
            .Select(i => Entry(Local(start.AddDays(i).AddHours(12)), 10))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(60);
    }

    [Fact]
    public void GetChartData_All_ShouldUseWeeklyCompression_WhenDailyDataExceedsSixty()
    {
        var sut = CreateSut();

        var start = new DateTime(2026, 1, 5);

        var entries = Enumerable.Range(0, 70)
            .Select(i => Entry(Local(start.AddDays(i).AddHours(12)), 10))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(10);

        result.DataPoints.Should().OnlyContain(e => Math.Abs(e.Value - 10) < 0.000000001);
    }

    [Fact]
    public void GetChartData_All_ShouldUseMonthlyCompression_WhenWeeklyDataStillExceedsSixty()
    {
        var sut = CreateSut();

        var start = new DateTime(2020, 1, 6);

        // one entry every week for 70 weeks:
        // daily = 70, weekly = 70, monthly < 60
        var entries = Enumerable.Range(0, 70)
            .Select(i => Entry(Local(start.AddDays(i * 7).AddHours(12)), 10))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCountLessThan(60);

        result.DataPoints.Should().OnlyContain(e => Math.Abs(e.Value - 10) < 0.000000001);

        var firstMonthStart = new DateTime(start.Year, start.Month, 1);

        result.DataPoints[0].Timestamp.Should().Be(LocalPeriodMidpoint(firstMonthStart, firstMonthStart.AddMonths(1)));
    }

    [Fact]
    public void GetChartData_All_ShouldUseYearlyCompression_WhenMonthlyDataStillExceedsSixty()
    {
        var sut = CreateSut();

        var start = new DateTime(2020, 1, 15);

        // 72 distinct months:
        // daily > 60, weekly > 60, monthly > 60, yearly = 6
        var entries = Enumerable.Range(0, 72)
            .Select(i => Entry(Local(start.AddMonths(i)), 10))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCount(6);

        result.DataPoints.Should().OnlyContain(e => Math.Abs(e.Value - 10) < 0.000000001);

        result.DataPoints[0].Timestamp.Should()
            .Be(LocalPeriodMidpoint(new DateTime(2020, 1, 1), new DateTime(2021, 1, 1)));
    }

    [Fact]
    public void GetChartData_All_ShouldApplyFinalCountCompression_WhenYearlyDataExceedsSixty()
    {
        var sut = CreateSut();

        // one entry in each of 61 different years is enough to force
        // daily -> weekly -> monthly -> yearly -> count compression
        var entries = Enumerable.Range(0, 61)
            .Select(i => Entry(Local(1960 + i, 7, 1, 12), 10))
            .ToList();

        var result = sut.GetChartData(ChartTimeRange.All, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().HaveCountLessThanOrEqualTo(60);
        result.DataPoints.Should().HaveCount(31);

        result.DataPoints.Should().OnlyContain(e => Math.Abs(e.Value - 10) < 0.000000001);

        result.DataPoints.Select(e => e.Timestamp!.Value).Should().BeInAscendingOrder();
    }

    #endregion

    #region Empty and filtered data

    [Theory]
    [InlineData(ChartTimeRange.Day)]
    [InlineData(ChartTimeRange.Week)]
    [InlineData(ChartTimeRange.Month)]
    [InlineData(ChartTimeRange.Year)]
    public void GetChartData_ShouldReturnEmptyData_WhenSelectedRangeContainsNoEntries(ChartTimeRange range)
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2000, 1, 1, 12), 10)
        };

        var result = sut.GetChartData(range, 0, TrendAggregation.Sum, entries);

        result.DataPoints.Should().BeEmpty();

        result.RangeStart.Should().NotBeNull();
        result.RangeEnd.Should().NotBeNull();
    }

    #endregion

    #region Input integrity

    [Fact]
    public void GetChartData_ShouldNotChangeOriginalEntryOrder()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var first = Entry(Local(2026, 9, 3, 18), 3);
        var second = Entry(Local(2026, 9, 3, 8), 1);
        var third = Entry(Local(2026, 9, 3, 12), 2);

        var entries = new List<NumericActivityDataEntry>
        {
            first,
            second,
            third
        };

        _ = sut.GetChartData(ChartTimeRange.Day, 0, TrendAggregation.Average, entries);

        entries.Should().ContainInOrder(first, second, third);
    }

    [Fact]
    public void GetChartData_ShouldNotModifyOriginalEntryValues()
    {
        var sut = CreateSut(Local(2026, 9, 3));

        var entries = new List<NumericActivityDataEntry>
        {
            Entry(Local(2026, 9, 2, 8), 10),
            Entry(Local(2026, 9, 2, 18), 20)
        };

        _ = sut.GetChartData(ChartTimeRange.Week, 0, TrendAggregation.Average, entries);

        entries[0].Value.Should().Be(10);
        entries[1].Value.Should().Be(20);
    }

    #endregion

    #region Helpers

    private static TrendActivityDataProvider CreateSut(DateTimeOffset? now = null)
    {
        now ??= Local(2026, 9, 3, 12);

        return new TrendActivityDataProvider(new TestTimeProvider(now.Value));
    }

    private static NumericActivityDataEntry Entry(DateTimeOffset timestamp, double value)
    {
        return new NumericActivityDataEntry
        {
            Timestamp = timestamp,
            Value = value
        };
    }

    private static List<NumericActivityDataEntry> EntriesForSameDay(IReadOnlyList<double> values)
    {
        return values
            .Select((value, index) => Entry(Local(2026, 9, 2, 8 + index * 3), value))
            .ToList();
    }

    private static DateTimeOffset Local(int year, int month, int day, int hour = 0, int minute = 0, int second = 0)
    {
        return Local(new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified));
    }

    private static DateTimeOffset Local(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        var offset = TimeZoneInfo.Local.GetUtcOffset(unspecified);

        return new DateTimeOffset(unspecified, offset);
    }

    private static DateTimeOffset LocalPeriodMidpoint(DateTime start, DateTime end)
    {
        return Midpoint(Local(start), Local(end));
    }

    private static DateTimeOffset Midpoint(DateTimeOffset start, DateTimeOffset end)
    {
        var startTicks = start.UtcDateTime.Ticks;
        var endTicks = end.UtcDateTime.Ticks;

        var midpointTicks = startTicks + (endTicks - startTicks) / 2;

        return FromUtcTicks(midpointTicks);
    }

    private static DateTimeOffset FromUtcTicks(long ticks)
    {
        return new DateTimeOffset(ticks, TimeSpan.Zero).ToLocalTime();
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public TestTimeProvider(DateTimeOffset localNow)
        {
            _utcNow = localNow.ToUniversalTime();
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
    }

    #endregion
}