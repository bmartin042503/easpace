// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents a single executable instruction of a wellness exercise with its resolved display text.
/// </summary>
/// <param name="Text">The instruction text shown to the user.</param>
/// <param name="DurationSeconds">The duration of the instruction in seconds.</param>
/// <param name="Phase">The breathing phase animated during the instruction, if any.</param>
internal sealed record ExerciseStep(string Text, int DurationSeconds, BreathingPhaseType? Phase)
{
    /// <summary>
    /// Creates a step, falling back to the localized default text of the breathing phase when no text is given.
    /// </summary>
    /// <param name="text">The user-defined instruction text; may be empty when a phase is set.</param>
    /// <param name="durationSeconds">The duration of the instruction in seconds.</param>
    /// <param name="phase">The optional breathing phase of the instruction.</param>
    /// <exception cref="ArgumentException">Thrown when the text is empty and no phase is set.</exception>
    public static ExerciseStep Create(string? text, int durationSeconds, BreathingPhaseType? phase)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            return new ExerciseStep(text.Trim(), durationSeconds, phase);
        }

        return phase is null
            ? throw new ArgumentException("Instruction text is required when no breathing phase is set.", nameof(text))
            : new ExerciseStep(GetDefaultText(phase), durationSeconds, phase);
    }

    /// <summary>
    /// Gets the localized default instruction text of a breathing phase, or an empty string when there is no phase.
    /// </summary>
    public static string GetDefaultText(BreathingPhaseType? phase)
    {
        return phase switch
        {
            BreathingPhaseType.Inhale => LocalizationService.GetString("Wellness.Instruction.BreatheIn"),
            BreathingPhaseType.HoldIn or BreathingPhaseType.HoldOut => LocalizationService.GetString(
                "Wellness.Instruction.Hold"),
            BreathingPhaseType.Exhale => LocalizationService.GetString("Wellness.Instruction.BreatheOut"),
            _ => string.Empty
        };
    }
}