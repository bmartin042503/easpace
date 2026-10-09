// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Mood.Validation;
using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Features.Mood.Validation;

public class MoodEntryValidatorTests
{
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_RejectsValuesOutsideTheExistingRange(double value)
    {
        MoodEntryValidator.Validate(value).Should().Equal(
            new ValidationIssue("MoodSliderValue", "Mood.Validation.Range"));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.5)]
    [InlineData(1d)]
    [InlineData(double.NaN)]
    public void Validate_PreservesAcceptedValuesIncludingNaN(double value)
    {
        MoodEntryValidator.Validate(value).Should().BeEmpty();
    }
}
