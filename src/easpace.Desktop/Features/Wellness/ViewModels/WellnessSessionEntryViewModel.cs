// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using easpace.Desktop.Features.Wellness.Constants;
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
    public WellnessSessionType SessionType { get; }
    public BreathingTechnique? BreathingTechnique { get; }

    public bool IsTypeBreathing => SessionType == WellnessSessionType.Breathing;
    public bool IsTypeMeditation => SessionType == WellnessSessionType.Meditation;

    public string? BreathingTechniqueName { get; init; }
    public string DurationMinutesText { get; init; }
    public string? CyclesText { get; init; }

    public WellnessSessionEntryViewModel(WellnessSessionEntry wellnessSessionEntry)
    {
        Id = wellnessSessionEntry.Id;
        StartDate = wellnessSessionEntry.StartDate;
        TargetDuration = wellnessSessionEntry.TargetDuration;
        ActualDuration = wellnessSessionEntry.ActualDuration;
        SessionType = wellnessSessionEntry.Type;
        BreathingTechnique = wellnessSessionEntry.BreathingTechnique;

        if (wellnessSessionEntry is { Type: WellnessSessionType.Breathing, BreathingTechnique: not null })
        {
            BreathingTechniqueName = BreathingTechnique!.IsLocalized
                ? LocalizationService.GetString(wellnessSessionEntry.BreathingTechnique.Name)
                : wellnessSessionEntry.BreathingTechnique.Name;

            var cycles = (int)(ActualDuration.TotalSeconds / BreathingTechnique.Phases.Sum(p => p.DurationSeconds));

            CyclesText = cycles == 1
                ? LocalizationService.GetString("Wellness.Session.OneCycle")
                : string.Format(LocalizationService.GetString("Wellness.Session.Cycles"), cycles);
        }

        var minutes = (int)ActualDuration.TotalMinutes;

        DurationMinutesText = minutes == 1
            ? LocalizationService.GetString("Common.Time.OneMinute")
            : string.Format(LocalizationService.GetString("Common.Time.Minutes"), minutes);
    }
}