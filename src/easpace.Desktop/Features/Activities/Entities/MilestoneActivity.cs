// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace easpace.Desktop.Features.Activities.Entities;

public class MilestoneActivity : NumericActivity
{
    public DateTimeOffset? TargetDate { get; set; }
}