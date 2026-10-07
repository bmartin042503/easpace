// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents the configuration parameters required to start a wellness session.
/// </summary>
/// <param name="Exercise">The exercise selected for the wellness session.</param>
/// <param name="TargetCycleCount">The number of cycles selected for the exercise.</param>
internal record WellnessSessionConfiguration(
    WellnessExercise Exercise,
    int TargetCycleCount
);