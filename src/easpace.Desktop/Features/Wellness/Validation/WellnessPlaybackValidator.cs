// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Validation;

internal static class WellnessPlaybackValidator
{
    public static bool IsPlayable(
        ICollection<WellnessExerciseInstruction> instructions,
        int cycleCount,
        bool validateBreathingPhases)
    {
        return cycleCount > 0 && instructions.Count > 0 && instructions.All(instruction =>
            instruction.DurationSeconds > 0
            && (!validateBreathingPhases || instruction.BreathingPhase is not { } phase || Enum.IsDefined(phase)));
    }
}