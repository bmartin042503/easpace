// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace easpace.Desktop.Features.Activities.Contracts;

internal record TrendChartDataPoint(DateTimeOffset? Timestamp, double Value);