// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Mood.Constants;

namespace easpace.Desktop.Features.Mood.Entities;

internal class MoodEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public double Value { get; set; }
    public string Description { get; set; } = string.Empty;
    public ICollection<MoodLabelState> Labels { get; set; } = [];
}
