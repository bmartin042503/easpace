// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.ViewModels.Dialogs;
using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Features.Activities;

public class ActivityEntryDialogViewModelTests
{
    [Fact]
    public void NumericEntry_InitializesAndUpdatesSharedErrorsAndConfirmAvailability()
    {
        var dialog = new NumericEntryDialogViewModel();
        dialog.HasErrors.Should().BeTrue();
        dialog.ConfirmCommand.CanExecute(null).Should().BeFalse();
        dialog.GetErrors(nameof(dialog.NumericValue)).Cast<ValidationIssue>().Should().Equal(
            new ValidationIssue(nameof(dialog.NumericValue), "FormValidation.NumericValue.Required"));

        var notifications = 0;
        dialog.ConfirmCommand.CanExecuteChanged += (_, _) => notifications++;
        dialog.NumericValue = 0;

        notifications.Should().BeGreaterThan(0);
        dialog.HasErrors.Should().BeFalse();
        dialog.GetErrors(nameof(dialog.NumericValue)).Cast<ValidationIssue>().Should().BeEmpty();
        dialog.ConfirmCommand.CanExecute(null).Should().BeTrue();

        dialog.SelectedDate = null;
        dialog.GetErrors(nameof(dialog.SelectedDate)).Cast<ValidationIssue>().Should().ContainSingle();
        dialog.ConfirmCommand.CanExecute(null).Should().BeFalse();

        dialog.SelectedDate = DateTime.Today;
        dialog.HasErrors.Should().BeFalse();
        dialog.NumericValue = null;
        dialog.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DirectConfirm_DoesNotCloseAnInvalidDialog(bool numeric)
    {
        EntryDialogViewModel dialog = numeric ? new NumericEntryDialogViewModel() : new RoutineEntryDialogViewModel();
        dialog.SelectedDate = null;
        dialog.Show();

        dialog.ConfirmCommand.Execute(null);

        dialog.Confirmed.Should().BeFalse();
        dialog.IsOpen.Should().BeTrue();

        dialog.SelectedDate = DateTime.Today;
        if (dialog is NumericEntryDialogViewModel numericDialog)
        {
            dialog.ConfirmCommand.Execute(null);
            dialog.Confirmed.Should().BeFalse();
            dialog.IsOpen.Should().BeTrue();
            numericDialog.NumericValue = -1;
        }

        dialog.ConfirmCommand.Execute(null);
        dialog.Confirmed.Should().BeTrue();
        dialog.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void DirectConfirm_RevalidatesEvenWhenPreviousErrorsWereCleared()
    {
        var dialog = new TestNumericEntryDialogViewModel { SelectedDate = null };
        dialog.ClearValidation();
        dialog.Show();
        dialog.ConfirmCommand.CanExecute(null).Should().BeTrue();

        dialog.ConfirmCommand.Execute(null);

        dialog.Confirmed.Should().BeFalse();
        dialog.IsOpen.Should().BeTrue();
        dialog.HasErrors.Should().BeTrue();
        dialog.GetErrors(nameof(dialog.SelectedDate)).Cast<ValidationIssue>().Should().Equal(
            new ValidationIssue(nameof(dialog.SelectedDate), "FormValidation.Date.Required"));
        dialog.GetErrors(nameof(dialog.NumericValue)).Cast<ValidationIssue>().Should().Equal(
            new ValidationIssue(nameof(dialog.NumericValue), "FormValidation.NumericValue.Required"));
        dialog.ConfirmCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RoutineEntry_PreservesDateClampingAndOptionalTime()
    {
        var dialog = new RoutineEntryDialogViewModel();
        dialog.ConfirmCommand.CanExecute(null).Should().BeTrue();

        dialog.SelectedDate = dialog.MaxAllowedDate.AddDays(2);
        dialog.SelectedTime = null;

        dialog.SelectedDate.Should().Be(dialog.MaxAllowedDate);
        dialog.HasErrors.Should().BeFalse();
        dialog.ConfirmCommand.CanExecute(null).Should().BeTrue();

        dialog.SelectedTime = TimeSpan.FromHours(12);
        dialog.GetTimestamp().DateTime.Should().Be(dialog.MaxAllowedDate.AddHours(12));
    }

    private sealed class TestNumericEntryDialogViewModel : NumericEntryDialogViewModel
    {
        public void ClearValidation() => SetValidationErrors([]);
    }
}
