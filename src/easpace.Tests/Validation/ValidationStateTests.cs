// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Validation;

public class ValidationStateTests
{
    [Fact]
    public void EmptyState_HasNoErrorsAndDoesNotNotifyForEmptyInput()
    {
        var state = new ValidationState();
        var changedMembers = new List<string?>();
        state.ErrorsChanged += (_, args) => changedMembers.Add(args.PropertyName);

        state.HasErrors.Should().BeFalse();
        state.GetErrors("Name").Should().BeEmpty();
        state.GetErrors(null).Should().BeEmpty();

        state.SetErrors([]);

        state.HasErrors.Should().BeFalse();
        changedMembers.Should().BeEmpty();
    }

    [Fact]
    public void SetErrors_GroupsErrorsAndCopiesTheInput()
    {
        var state = new ValidationState();
        var required = new ValidationIssue("Name", "Test.Validation.Required");
        var tooShort = new ValidationIssue("Name", "Test.Validation.TooShort");
        var positive = new ValidationIssue("Duration", "Test.Validation.Positive");
        var issues = new List<ValidationIssue> { required, positive, tooShort };
        var changedMembers = new List<string?>();
        state.ErrorsChanged += (_, args) => changedMembers.Add(args.PropertyName);

        state.SetErrors(issues);
        issues.Clear();

        state.HasErrors.Should().BeTrue();
        state.GetErrors("Name").Should().Equal(required, tooShort);
        state.GetErrors("Duration").Should().Equal(positive);
        state.GetErrors("Unknown").Should().BeEmpty();
        changedMembers.Should().BeEquivalentTo(new[] { "Name", "Duration" });
    }

    [Fact]
    public void SetErrors_NotifiesChangedAddedAndRemovedMembersAfterReplacingState()
    {
        var state = new ValidationState();
        state.SetErrors([
            new ValidationIssue("Name", "Test.Validation.Required"),
            new ValidationIssue("Description", "Test.Validation.TooLong"),
            new ValidationIssue("Duration", "Test.Validation.Positive")
        ]);

        var nameError = new ValidationIssue("Name", "Test.Validation.TooShort");
        var durationError = new ValidationIssue("Duration", "Test.Validation.Positive");
        var cyclesError = new ValidationIssue("Cycles", "Test.Validation.Positive");
        var changedMembers = new List<string?>();

        state.ErrorsChanged += (_, args) =>
        {
            changedMembers.Add(args.PropertyName);
            state.HasErrors.Should().BeTrue();
            state.GetErrors("Name").Should().Equal(nameError);
            state.GetErrors("Description").Should().BeEmpty();
            state.GetErrors("Duration").Should().Equal(durationError);
            state.GetErrors("Cycles").Should().Equal(cyclesError);
        };

        state.SetErrors([nameError, durationError, cyclesError]);

        changedMembers.Should().BeEquivalentTo(new[] { "Name", "Description", "Cycles" });
    }

    [Fact]
    public void SetErrors_WithEquivalentResultsInDifferentFieldOrder_DoesNotNotify()
    {
        var state = new ValidationState();
        state.SetErrors([
            new ValidationIssue("Name", "Test.Validation.Required"),
            new ValidationIssue("Name", "Test.Validation.TooShort"),
            new ValidationIssue("Duration", "Test.Validation.Positive")
        ]);

        var changedMembers = new List<string?>();
        state.ErrorsChanged += (_, args) => changedMembers.Add(args.PropertyName);

        state.SetErrors([
            new ValidationIssue("Duration", "Test.Validation.Positive"),
            new ValidationIssue("Name", "Test.Validation.Required"),
            new ValidationIssue("Name", "Test.Validation.TooShort")
        ]);

        state.HasErrors.Should().BeTrue();
        changedMembers.Should().BeEmpty();
    }

    [Fact]
    public void SetErrors_WithChangedErrorOrderWithinOneMember_Notifies()
    {
        var state = new ValidationState();
        var required = new ValidationIssue("Name", "Test.Validation.Required");
        var tooShort = new ValidationIssue("Name", "Test.Validation.TooShort");
        state.SetErrors([required, tooShort]);

        var changedMembers = new List<string?>();
        state.ErrorsChanged += (_, args) => changedMembers.Add(args.PropertyName);

        state.SetErrors([tooShort, required]);

        state.GetErrors("Name").Should().Equal(tooShort, required);
        changedMembers.Should().Equal("Name");
    }

    [Fact]
    public void SetErrors_WithEmptyInput_ClearsErrorsAndNotifiesRemovedMembersOnce()
    {
        var state = new ValidationState();
        state.SetErrors([
            new ValidationIssue("Name", "Test.Validation.Required"),
            new ValidationIssue(string.Empty, "Test.Validation.Invalid")
        ]);

        var changedMembers = new List<string?>();
        state.ErrorsChanged += (_, args) =>
        {
            changedMembers.Add(args.PropertyName);
            state.HasErrors.Should().BeFalse();
            state.GetErrors("Name").Should().BeEmpty();
            state.GetErrors(null).Should().BeEmpty();
        };

        state.SetErrors([]);

        state.HasErrors.Should().BeFalse();
        changedMembers.Should().BeEquivalentTo("Name", string.Empty);

        changedMembers.Clear();
        state.SetErrors([]);

        changedMembers.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetErrors_WithNullOrEmptyMember_ReturnsOnlyFormErrors(string? propertyName)
    {
        var state = new ValidationState();
        var fieldError = new ValidationIssue("Name", "Test.Validation.Required");
        var formError = new ValidationIssue(string.Empty, "Test.Validation.Invalid");

        state.SetErrors([fieldError]);

        state.GetErrors(propertyName).Should().BeEmpty();

        state.SetErrors([fieldError, formError]);

        state.GetErrors(propertyName).Should().Equal(formError);
        state.GetErrors("Name").Should().Equal(fieldError);
    }
}