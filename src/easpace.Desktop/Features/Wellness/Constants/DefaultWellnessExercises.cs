// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Features.Wellness.Constants;

/// <summary>
/// Provides the built-in exercises used for seeding.
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
            CreateBreathing("DefaultExercise.BoxBreathing",
                (BreathingPhaseType.Inhale, 4), (BreathingPhaseType.HoldIn, 4),
                (BreathingPhaseType.Exhale, 4), (BreathingPhaseType.HoldOut, 4)),

            CreateBreathing("DefaultExercise.FourSevenEightBreathing",
                (BreathingPhaseType.Inhale, 4), (BreathingPhaseType.HoldIn, 7), (BreathingPhaseType.Exhale, 8)),

            CreateBreathing("DefaultExercise.FourSixBreathing",
                (BreathingPhaseType.Inhale, 4), (BreathingPhaseType.Exhale, 6)),

            CreateBreathing("DefaultExercise.TriangleBreathing",
                (BreathingPhaseType.Inhale, 3), (BreathingPhaseType.HoldIn, 3), (BreathingPhaseType.Exhale, 3)),

            CreateMeditation("DefaultExercise.GuidedCalm",
                "DefaultExercise.MeditationPrompt.FocusOnBreath",
                "DefaultExercise.MeditationPrompt.NoticeSensations",
                "DefaultExercise.MeditationPrompt.ReturnToPresent",
                "DefaultExercise.MeditationPrompt.LetThoughtsPass",
                "DefaultExercise.MeditationPrompt.RelaxShoulders",
                "DefaultExercise.MeditationPrompt.SimplyBeHere"),

            // a single one-minute instruction lets users pick the session length in minutes
            CreateMeditation("DefaultExercise.SilentSitting", "DefaultExercise.MeditationPrompt.SimplyBeHere")
        ];
    }

    private static WellnessExercise CreateBreathing(
        string keyPrefix,
        params (BreathingPhaseType Phase, int DurationSeconds)[] phases)
    {
        return new WellnessExercise
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

    private static WellnessExercise CreateMeditation(string keyPrefix, params string[] instructionKeys)
    {
        return new WellnessExercise
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