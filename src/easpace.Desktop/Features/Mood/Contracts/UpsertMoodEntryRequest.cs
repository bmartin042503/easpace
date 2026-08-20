// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Mood.Constants;

namespace easpace.Desktop.Features.Mood.Contracts;

public sealed record UpsertMoodEntryRequest(
    DateTimeOffset Timestamp,
    double Value,
    string Description,
    ICollection<MoodLabelState> Labels
);