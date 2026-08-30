// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace easpace.Desktop.Features.Activities.Entities;

internal class MilestoneActivity : NumericActivity
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetDate { get; set; }
}