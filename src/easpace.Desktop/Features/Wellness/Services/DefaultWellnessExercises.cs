// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Features.Wellness.Services;

/// <summary>
/// Provides the built-in exercises used for seeding and restoring defaults.
/// </summary>
internal static class DefaultWellnessExercises
{
    private const int MeditationInstructionSeconds = 60;

    /// <summary>
    /// Creates new, untracked instances of the built-in exercises with texts in the current app language.
    /// </summary>
    public static IReadOnlyList<WellnessExercise> Create()
    {
        return
        [
            CreateBreathing("BreathingTechnique.BoxBreathing",
                (BreathingPhaseType.Inhale, 4), (BreathingPhaseType.HoldIn, 4),
                (BreathingPhaseType.Exhale, 4), (BreathingPhaseType.HoldOut, 4)),

            CreateBreathing("BreathingTechnique.FourSevenEightBreathing",
                (BreathingPhaseType.Inhale, 4), (BreathingPhaseType.HoldIn, 7), (BreathingPhaseType.Exhale, 8)),

            CreateBreathing("BreathingTechnique.FourSixBreathing",
                (BreathingPhaseType.Inhale, 4), (BreathingPhaseType.Exhale, 6)),

            CreateBreathing("BreathingTechnique.TriangleBreathing",
                (BreathingPhaseType.Inhale, 3), (BreathingPhaseType.HoldIn, 3), (BreathingPhaseType.Exhale, 3)),

            CreateMeditation("Wellness.Default.GuidedCalm",
                "Wellness.Instruction1.Meditation", "Wellness.Instruction2.Meditation", "Wellness.Instruction3.Meditation",
                "Wellness.Instruction4.Meditation", "Wellness.Instruction5.Meditation", "Wellness.Instruction6.Meditation"),

            // a single one-minute instruction lets users pick the session length in minutes
            CreateMeditation("Wellness.Default.SilentSitting", "Wellness.Instruction6.Meditation")
        ];
    }

    private static BreathingExercise CreateBreathing(
        string keyPrefix,
        params (BreathingPhaseType Phase, int DurationSeconds)[] phases)
    {
        return new BreathingExercise
        {
            Name = LocalizationService.GetString($"{keyPrefix}.Name"),
            Description = LocalizationService.GetString($"{keyPrefix}.Description"),
            IsRepeating = true,
            Instructions = phases
                .Select((phase, index) => new ExerciseInstruction
                {
                    Order = index + 1,
                    Text = ExerciseStep.GetDefaultText(phase.Phase),
                    DurationSeconds = phase.DurationSeconds,
                    Phase = phase.Phase
                })
                .ToList()
        };
    }

    private static MeditationExercise CreateMeditation(string keyPrefix, params string[] instructionKeys)
    {
        return new MeditationExercise
        {
            Name = LocalizationService.GetString($"{keyPrefix}.Name"),
            Description = LocalizationService.GetString($"{keyPrefix}.Description"),
            IsRepeating = true,
            Instructions = instructionKeys
                .Select((key, index) => new ExerciseInstruction
                {
                    Order = index + 1,
                    Text = LocalizationService.GetString(key),
                    DurationSeconds = MeditationInstructionSeconds
                })
                .ToList()
        };
    }
}
