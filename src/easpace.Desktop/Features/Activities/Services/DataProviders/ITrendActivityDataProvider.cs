// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

public interface ITrendActivityDataProvider
{
    List<TrendChartDataPoint> GetChartData(ChartTimeRange chartTimeRange, List<NumericDataEntry> numericEntries);
}