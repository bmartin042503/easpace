// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Features.Activities.Entities;

public abstract class NumericActivity : Activity
{
    public double? Target { get; set; }
    public string? Unit { get; set; }
}