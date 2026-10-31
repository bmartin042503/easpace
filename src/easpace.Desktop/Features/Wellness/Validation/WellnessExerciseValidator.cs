// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Validation;

namespace easpace.Desktop.Features.Wellness.Validation;

internal static class WellnessExerciseValidator
{
    public static IEnumerable<ValidationIssue> Validate(string? name, int defaultCycleCount, int instructionCount)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return new ValidationIssue("Name", "Validation.Name.Required");
        }
        else if (name.Length < 3)
        {
            yield return new ValidationIssue("Name", "Validation.Name.MinLength");
        }
        else if (name.Length > 64)
        {
            yield return new ValidationIssue("Name", "Validation.Name.MaxLength");
        }

        if (defaultCycleCount is < 1 or > 99)
        {
            yield return new ValidationIssue("DefaultCycleCount", "Validation.CycleCount.Range");
        }

        if (instructionCount == 0)
        {
            yield return new ValidationIssue("Instructions", "Validation.Instructions.Required");
        }
    }

    public static IEnumerable<ValidationIssue> ValidateInstruction(
        string? text,
        int durationSeconds,
        BreathingPhase breathingPhase)
    {
        if (durationSeconds is < 1 or > 600)
        {
            yield return new ValidationIssue("DurationSeconds", "Validation.Duration.Range");
        }

        // an instruction without text only makes sense when the breathing phase gives it a default text
        if (string.IsNullOrWhiteSpace(text) && breathingPhase == BreathingPhase.None)
        {
            yield return new ValidationIssue("Text", "Validation.Instruction.ContentRequired");
        }
    }
}
