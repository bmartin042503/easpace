// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using easpace.Desktop.Converters;
using easpace.Desktop.Validation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using FluentAssertions;

namespace easpace.Tests.Validation;

public class ViewModelValidationTests
{
    [Fact]
    public void SetValidationErrors_ForwardsEventsFromTheViewModelWithTheNewState()
    {
        var viewModel = new TestPageViewModel();
        var validation = (INotifyDataErrorInfo)viewModel;
        var issue = new ValidationIssue(nameof(viewModel.Name), "FormValidation.Name.Required");
        var formIssue = new ValidationIssue(string.Empty, "Test.Validation.Invalid");
        var changedMembers = new List<string?>();

        validation.ErrorsChanged += (sender, args) =>
        {
            sender.Should().BeSameAs(viewModel);
            validation.HasErrors.Should().BeTrue();
            validation.GetErrors(nameof(viewModel.Name)).Cast<ValidationIssue>().Should().Equal(issue);
            validation.GetErrors(null).Cast<ValidationIssue>().Should().Equal(formIssue);
            changedMembers.Add(args.PropertyName);
        };

        viewModel.ApplyErrors([issue, formIssue]);
        viewModel.ApplyErrors([issue, formIssue]);

        changedMembers.Should().BeEquivalentTo(new[] { nameof(viewModel.Name), string.Empty });
    }

    [Fact]
    public void SetValidationErrors_NotifiesHasErrorsOnlyWhenItsValueChanges()
    {
        var viewModel = new TestPageViewModel();
        var changes = new List<bool>();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(viewModel.HasErrors)) changes.Add(viewModel.HasErrors);
        };

        viewModel.ApplyErrors([]);
        changes.Should().BeEmpty();

        viewModel.ApplyErrors([new ValidationIssue("Name", "FormValidation.Name.Required")]);
        viewModel.ApplyErrors([new ValidationIssue("Name", "FormValidation.Name.MinLength")]);
        changes.Should().Equal(true);

        viewModel.ApplyErrors([]);
        viewModel.ApplyErrors([]);
        changes.Should().Equal(true, false);
    }

    [AvaloniaFact]
    public void TextBoxBindings_InheritValidationFromPageAndDialogAndClearRemovedErrors()
    {
        var page = new TestPageViewModel();
        var dialog = new TestDialogViewModel();
        var issue = new ValidationIssue("Name", "FormValidation.Name.Required");
        page.ApplyErrors([issue]);

        var pageTextBox = new TextBox();
        var dialogTextBox = new TextBox();
        using var pageBinding = pageTextBox.Bind(TextBox.TextProperty,
            new Binding(nameof(page.Name)) { Source = page, Mode = BindingMode.TwoWay });
        using var dialogBinding = dialogTextBox.Bind(TextBox.TextProperty,
            new Binding(nameof(dialog.Name)) { Source = dialog, Mode = BindingMode.TwoWay });
        Dispatcher.UIThread.RunJobs();

        DataValidationErrors.GetHasErrors(pageTextBox).Should().BeTrue();
        DataValidationErrors.GetErrors(pageTextBox).Should().Equal(issue);
        DataValidationErrors.GetHasErrors(dialogTextBox).Should().BeFalse();

        page.ApplyErrors([]);
        dialog.ApplyErrors([issue]);
        Dispatcher.UIThread.RunJobs();

        DataValidationErrors.GetHasErrors(pageTextBox).Should().BeFalse();
        DataValidationErrors.GetHasErrors(dialogTextBox).Should().BeTrue();
        DataValidationErrors.GetErrors(dialogTextBox).Should().Equal(issue);

        dialog.ApplyErrors([]);
        Dispatcher.UIThread.RunJobs();

        DataValidationErrors.GetHasErrors(dialogTextBox).Should().BeFalse();
    }

    [AvaloniaFact]
    public void InvalidNumericBinding_PreservesTheValueAndDisplaysTheConversionMessage()
    {
        var viewModel = new TestPageViewModel();
        var textBox = new TextBox();
        using var binding = textBox.Bind(TextBox.TextProperty,
            new Binding(nameof(viewModel.Count)) { Source = viewModel, Mode = BindingMode.TwoWay });
        Dispatcher.UIThread.RunJobs();

        textBox.Text = "not a number";
        Dispatcher.UIThread.RunJobs();

        viewModel.Count.Should().Be(3);
        DataValidationErrors.GetHasErrors(textBox).Should().BeTrue();
        var error = DataValidationErrors.GetErrors(textBox)!.OfType<Exception>().Should().ContainSingle().Subject;
        error.Message.Should().NotBeNullOrWhiteSpace();
        var converter = new ValidationResultToLocalizedConverter();
        converter.Convert(error, typeof(string), null, CultureInfo.CurrentCulture).Should().Be(error.Message);
        viewModel.HasErrors.Should().BeFalse();

        textBox.Text = "4";
        Dispatcher.UIThread.RunJobs();

        viewModel.Count.Should().Be(4);
        DataValidationErrors.GetHasErrors(textBox).Should().BeFalse();
    }

    private sealed class TestPageViewModel : PageViewModel
    {
        public string Name { get; set; } = "Name";
        public int Count { get; set; } = 3;

        public void ApplyErrors(IEnumerable<ValidationIssue> issues) => SetValidationErrors(issues);
    }

    private sealed class TestDialogViewModel : DialogViewModel
    {
        public string Name { get; set; } = "Name";

        public void ApplyErrors(IEnumerable<ValidationIssue> issues) => SetValidationErrors(issues);
    }
}
