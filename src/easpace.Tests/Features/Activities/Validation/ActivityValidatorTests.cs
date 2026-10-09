// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Validation;
using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Features.Activities.Validation;

public class ActivityValidatorTests
{
    [Theory]
    [InlineData(null, "FormValidation.Name.Required")]
    [InlineData("", "FormValidation.Name.Required")]
    [InlineData("   ", "FormValidation.Name.Required")]
    [InlineData("ab", "FormValidation.Name.MinLength")]
    public void Validate_RejectsMissingOrShortNames(string? name, string key)
    {
        var input = new ActivityValidationInput(name, ActivityType.Trend, null, null, false);

        ActivityValidator.Validate(input).Should().Equal(new ValidationIssue("Name", key));
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(64, true)]
    [InlineData(65, false)]
    public void Validate_EnforcesNameLengthBoundaries(int length, bool valid)
    {
        var input = new ActivityValidationInput(new string('a', length), ActivityType.Routine, null, null, false);
        var errors = ActivityValidator.Validate(input).ToArray();

        if (valid) errors.Should().BeEmpty();
        else errors.Should().Equal(new ValidationIssue("Name", "FormValidation.Name.MaxLength"));
    }

    [Theory]
    [InlineData(ActivityType.Trend)]
    [InlineData(ActivityType.Milestone)]
    public void Validate_EnforcesUnitLengthOnlyForNumericActivities(ActivityType type)
    {
        var input = new ActivityValidationInput("Walking", type, 1, new string('a', 16), true);

        ActivityValidator.Validate(input).Should().BeEmpty();
        ActivityValidator.Validate(input with { Unit = new string('a', 17) })
            .Should().Equal(new ValidationIssue("Unit", "FormValidation.Unit.MaxLength"));
        ActivityValidator.Validate(input with { Unit = null }).Should().BeEmpty();
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(0d)]
    [InlineData(10_000_001d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_RejectsInvalidActiveTargets(double target)
    {
        foreach (var type in new[] { ActivityType.Trend, ActivityType.Milestone })
        {
            var input = new ActivityValidationInput("Walking", type, target, null, true);

            ActivityValidator.Validate(input).Should().Equal(new ValidationIssue("Target", "FormValidation.Target.Range"));
        }
    }

    [Theory]
    [InlineData(0.5d)]
    [InlineData(1d)]
    [InlineData(10_000_000d)]
    public void Validate_PreservesPositiveFractionalTargetsAndTheUpperBoundary(double target)
    {
        var input = new ActivityValidationInput("Walking", ActivityType.Milestone, target, null, false);

        ActivityValidator.Validate(input).Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_RequiresAMilestoneTargetRegardlessOfTheCheckbox(bool isTargetChecked)
    {
        var input = new ActivityValidationInput("Walking", ActivityType.Milestone, null, null, isTargetChecked);

        ActivityValidator.Validate(input).Should().Equal(new ValidationIssue("Target", "FormValidation.Target.Required"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Validate_PreservesAnEmptyOptionalTrendTarget(bool isTargetChecked)
    {
        var input = new ActivityValidationInput("Walking", ActivityType.Trend, null, null, isTargetChecked);

        ActivityValidator.Validate(input).Should().BeEmpty();
    }

    [Fact]
    public void Validate_IgnoresInactiveFieldsWithoutChangingTheInput()
    {
        var input = new ActivityValidationInput(" Walking ", ActivityType.Routine, -1, new string('a', 17), true);

        ActivityValidator.Validate(input).Should().BeEmpty();
        ActivityValidator.Validate(input with { Type = ActivityType.Trend, Unit = null, IsTargetChecked = false })
            .Should().BeEmpty();
        input.Name.Should().Be(" Walking ");
        input.Target.Should().Be(-1);
        input.Unit.Should().HaveLength(17);
    }
}
