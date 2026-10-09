// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.Validation;
using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Features.Activities.Validation;

public class ActivityEntryValidatorTests
{
    [Fact]
    public void Validate_RequiresOnlyADateForRoutineEntries()
    {
        ActivityEntryValidator.Validate(null).Should().Equal(
            new ValidationIssue("SelectedDate", "FormValidation.Date.Required"));
        ActivityEntryValidator.Validate(new DateTime(2026, 10, 9)).Should().BeEmpty();
    }

    [Fact]
    public void Validate_RequiresBothDateAndValueForNumericEntries()
    {
        ActivityEntryValidator.Validate(null, null).Should().Equal(
            new ValidationIssue("SelectedDate", "FormValidation.Date.Required"),
            new ValidationIssue("NumericValue", "FormValidation.NumericValue.Required"));
        ActivityEntryValidator.Validate(new DateTime(2026, 10, 9), null).Should().Equal(
            new ValidationIssue("NumericValue", "FormValidation.NumericValue.Required"));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_PreservesTheExistingValuePresenceRule(double value)
    {
        ActivityEntryValidator.Validate(new DateTime(2026, 10, 9), value).Should().BeEmpty();
    }
}
