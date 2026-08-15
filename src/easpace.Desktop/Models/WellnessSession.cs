// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models;

public class WellnessSession
{
    public Guid Id { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public TimeSpan TargetDuration { get; set; }
    public TimeSpan ActualDuration { get; set; }
    public WellnessSessionType Type { get; set; }
    public BreathingTechnique? BreathingTechnique { get; set; }
}