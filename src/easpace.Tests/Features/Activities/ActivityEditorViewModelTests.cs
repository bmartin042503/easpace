// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using easpace.Desktop.Features.Activities.Repositories;
using easpace.Desktop.Features.Activities.ViewModels;
using easpace.Desktop.Services.Presentation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Activities;

public class ActivityEditorViewModelTests
{
    [Fact]
    public void NameChanges_KeepLegacyValidationAndSaveAvailabilityWorking()
    {
        var editor = new ActivityEditorViewModel(
            Mock.Of<IActivityRepository>(), Mock.Of<IDialogService>(), Mock.Of<ILogger<ActivityEditorViewModel>>());
        var validation = (INotifyDataErrorInfo)editor;

        editor.Name = string.Empty;

        validation.HasErrors.Should().BeTrue();
        validation.GetErrors(nameof(editor.Name)).Cast<ValidationResult>()
            .Should().Contain(error => error.ErrorMessage == "FormValidation.Name.Required");
        editor.SaveCommand.CanExecute(null).Should().BeFalse();

        editor.Name = "Walking";

        validation.HasErrors.Should().BeFalse();
        validation.GetErrors(nameof(editor.Name)).Cast<ValidationResult>().Should().BeEmpty();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
    }
}
