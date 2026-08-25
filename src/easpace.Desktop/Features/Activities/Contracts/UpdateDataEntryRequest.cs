// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Activities.Constants;

namespace easpace.Desktop.Features.Activities.Contracts;

internal sealed record UpdateDataEntryRequest(
    DateTimeOffset? Timestamp,
    double? Value,
    RoutineState? State
);