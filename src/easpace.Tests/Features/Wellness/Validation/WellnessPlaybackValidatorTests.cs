// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Validation;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness.Validation;

public class WellnessPlaybackValidatorTests
{
    [Theory]
    [InlineData(0, 4)]
    [InlineData(-1, 4)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void IsPlayable_RequiresPositiveCyclesAndEveryDuration(int cycles, int duration)
    {
        WellnessExerciseInstruction[] instructions =
        [
            new() { DurationSeconds = 4 },
            new() { DurationSeconds = duration }
        ];

        WellnessPlaybackValidator.IsPlayable(instructions, cycles, validateBreathingPhases: false).Should().BeFalse();
        WellnessPlaybackValidator.IsPlayable(instructions, cycles, validateBreathingPhases: true).Should().BeFalse();
    }

    [Fact]
    public void IsPlayable_RejectsEmptyInstructions()
    {
        WellnessPlaybackValidator.IsPlayable([], 1, validateBreathingPhases: false).Should().BeFalse();
        WellnessPlaybackValidator.IsPlayable([], 1, validateBreathingPhases: true).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(999, false)]
    public void IsPlayable_PreservesOptionalPhaseAndCallerSpecificEnumChecks(int? phase,
        bool expectedWithPhaseValidation)
    {
        WellnessExerciseInstruction[] instructions =
        [
            new() { DurationSeconds = 1, BreathingPhase = (BreathingPhase?)phase }
        ];

        WellnessPlaybackValidator.IsPlayable(instructions, 1, validateBreathingPhases: false).Should().BeTrue();
        WellnessPlaybackValidator.IsPlayable(instructions, 1, validateBreathingPhases: true).Should()
            .Be(expectedWithPhaseValidation);
    }
}