// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Entities;

internal abstract class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public string Name { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public ICollection<ActivityDataEntry> Entries { get; set; } = [];
}