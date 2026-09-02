// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace easpace.Desktop.Features.Activities.Contracts;

internal record TrendChartData(
    IReadOnlyList<TrendChartDataPoint> DataPoints,
    DateTimeOffset? RangeStart,
    DateTimeOffset? RangeEnd
);