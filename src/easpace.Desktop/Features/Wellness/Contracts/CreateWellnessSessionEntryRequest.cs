// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Contracts;

internal sealed record CreateWellnessSessionEntryRequest(
    DateTimeOffset StartDate,
    TimeSpan? TargetDuration,
    TimeSpan ActualDuration,
    WellnessSessionType SessionType,
    BreathingTechnique? BreathingTechnique
);