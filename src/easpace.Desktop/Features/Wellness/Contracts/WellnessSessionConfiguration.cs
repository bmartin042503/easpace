// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents the configuration parameters required to start a wellness session.
/// </summary>
/// <param name="Exercise">The exercise of the wellness session.</param>
/// <param name="TargetDuration">The target duration for the session.</param>
internal record WellnessSessionConfiguration(
    WellnessExercise Exercise,
    TimeSpan? TargetDuration
);