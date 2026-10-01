// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.Linq;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services.Core;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Wellness.ViewModels;

internal class WellnessSessionEntryViewModel : ViewModelBase
{
    public Guid Id { get; }
    public DateTimeOffset StartDate { get; }
    public TimeSpan? TargetDuration { get; }
    public TimeSpan ActualDuration { get; }

    /// <summary>
    /// Gets the exercise name, the duration and, while the exercise exists and repeats, the cycle count, joined with " • ".
    /// </summary>
    public string DetailsText { get; }

    public string TimestampText => StartDate.ToLocalTime().ToString("F", CultureInfo.CurrentCulture);

    public WellnessSessionEntryViewModel(WellnessSessionEntry wellnessSessionEntry)
    {
        Id = wellnessSessionEntry.Id;
        StartDate = wellnessSessionEntry.StartDate;
        TargetDuration = wellnessSessionEntry.TargetDuration;
        ActualDuration = wellnessSessionEntry.ActualDuration;

        // the name saved with the session comes first, so renaming or deleting the exercise keeps the history intact
        var exerciseName = wellnessSessionEntry.ExerciseName ?? wellnessSessionEntry.Exercise?.Name;

        string?[] parts = [exerciseName, FormatDuration(ActualDuration), GetCyclesText(wellnessSessionEntry)];
        DetailsText = string.Join(" • ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalHours >= 1
            ? FormattableString.Invariant($"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}")
            : FormattableString.Invariant($"{duration.Minutes:00}:{duration.Seconds:00}");

    /// <summary>
    /// Recomputes the completed cycles from the exercise's current instructions. That needs the exercise to still exist,
    /// and cycles are only meaningful when it repeats.
    /// </summary>
    private static string? GetCyclesText(WellnessSessionEntry entry)
    {
        if (entry.Exercise is not { IsRepeating: true } exercise) return null;

        var cycleSeconds = exercise.Instructions.Sum(i => i.DurationSeconds);
        if (cycleSeconds <= 0) return null;

        var cycles = (int)(entry.ActualDuration.TotalSeconds / cycleSeconds);

        return cycles == 1
            ? LocalizationService.GetString("Wellness.Session.OneCycle")
            : string.Format(LocalizationService.GetString("Wellness.Session.Cycles"), cycles);
    }
}