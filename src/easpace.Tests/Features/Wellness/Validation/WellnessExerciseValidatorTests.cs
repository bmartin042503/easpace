// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Validation;
using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness.Validation;

public class WellnessExerciseValidatorTests
{
    [Fact]
    public void Validate_AcceptsAValidExercise()
    {
        WellnessExerciseValidator.Validate("Box Breathing", 4, 4).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Validation.Name.Required")]
    [InlineData("", "Validation.Name.Required")]
    [InlineData("   ", "Validation.Name.Required")]
    [InlineData("ab", "Validation.Name.MinLength")]
    public void Validate_RejectsMissingOrShortNames(string? name, string key)
    {
        WellnessExerciseValidator.Validate(name, 1, 1).Should().Equal(new ValidationIssue("Name", key));
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void Validate_EnforcesNameLengthBoundaries(int length, bool valid)
    {
        var errors = WellnessExerciseValidator.Validate(new string('a', length), 1, 1).ToArray();

        if (valid) errors.Should().BeEmpty();
        else errors.Should().Equal(new ValidationIssue("Name", "Validation.Name.MaxLength"));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(99, true)]
    [InlineData(100, false)]
    public void Validate_EnforcesCycleCountBoundaries(int cycleCount, bool valid)
    {
        var errors = WellnessExerciseValidator.Validate("Box Breathing", cycleCount, 1).ToArray();

        if (valid) errors.Should().BeEmpty();
        else errors.Should().Equal(new ValidationIssue("DefaultCycleCount", "Validation.CycleCount.Range"));
    }

    [Fact]
    public void Validate_RequiresAtLeastOneInstruction()
    {
        WellnessExerciseValidator.Validate("Box Breathing", 1, 0)
            .Should().Equal(new ValidationIssue("Instructions", "Validation.Instructions.Required"));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(600, true)]
    [InlineData(601, false)]
    public void ValidateInstruction_EnforcesDurationBoundaries(int durationSeconds, bool valid)
    {
        var errors = WellnessExerciseValidator.ValidateInstruction("Breathe in", durationSeconds, BreathingPhase.None)
            .ToArray();

        if (valid) errors.Should().BeEmpty();
        else errors.Should().Equal(new ValidationIssue("DurationSeconds", "Validation.Duration.Range"));
    }

    [Theory]
    [InlineData(null, (int)BreathingPhase.None, false)]
    [InlineData("", (int)BreathingPhase.None, false)]
    [InlineData("   ", (int)BreathingPhase.None, false)]
    [InlineData(null, (int)BreathingPhase.Inhale, true)]
    [InlineData("", (int)BreathingPhase.HoldOut, true)]
    [InlineData("Notice your breath", (int)BreathingPhase.None, true)]
    public void ValidateInstruction_RequiresATextOrABreathingPhase(string? text, int phase, bool valid)
    {
        var errors = WellnessExerciseValidator.ValidateInstruction(text, 5, (BreathingPhase)phase).ToArray();

        if (valid) errors.Should().BeEmpty();
        else errors.Should().Equal(new ValidationIssue("Text", "Validation.Instruction.ContentRequired"));
    }
}
