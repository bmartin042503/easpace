// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services.Core;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal class WellnessSessionEntryViewModel : ViewModelBase
{
    public Guid Id { get; }
    public DateTimeOffset StartDate { get; }
    public TimeSpan Duration { get; }
    public string ExerciseName { get; }
    public WellnessExercise? Exercise { get; }

    public string DurationText =>
        Duration.TotalHours >= 1
            ? FormattableString.Invariant(
                $"{(long)Duration.TotalHours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}")
            : FormattableString.Invariant(
                $"{Duration.Minutes:00}:{Duration.Seconds:00}");

    public string? CyclesText { get; init; }

    public string TimestampText => StartDate.ToLocalTime().ToString("F", CultureInfo.CurrentCulture);

    public WellnessSessionEntryViewModel(WellnessSessionEntry wellnessSessionEntry)
    {
        Id = wellnessSessionEntry.Id;
        StartDate = wellnessSessionEntry.StartDate;
        Duration = wellnessSessionEntry.Duration;
        ExerciseName = wellnessSessionEntry.ExerciseName;
        Exercise = wellnessSessionEntry.Exercise;

        CyclesText = wellnessSessionEntry.CycleCount == 1
            ? LocalizationService.GetString("Wellness.Session.OneCycle")
            : string.Format(LocalizationService.GetString("Wellness.Session.Cycles"), wellnessSessionEntry.CycleCount);
    }
}