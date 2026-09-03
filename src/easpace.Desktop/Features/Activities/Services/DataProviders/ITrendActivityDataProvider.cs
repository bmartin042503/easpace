// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

internal interface ITrendActivityDataProvider
{
    TrendChartData GetChartData(
        ChartTimeRange chartTimeRange,
        int intervalsBack,
        TrendAggregation aggregation,
        List<NumericActivityDataEntry> numericEntries);
    
    double? GetDailyValue(
        DateTimeOffset date,
        TrendAggregation aggregation,
        IEnumerable<NumericActivityDataEntry> numericEntries);
}