// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.DataProviders;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Repositories;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.ViewModels;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Activities;

public class ActivityEditorViewModelTests
{
    [Fact]
    public void NameChanges_UpdateSharedErrorsAndSaveAvailability()
    {
        var editor = CreateEditor();
        var validation = (INotifyDataErrorInfo)editor;

        editor.Name = string.Empty;

        validation.HasErrors.Should().BeTrue();
        validation.GetErrors(nameof(editor.Name)).Cast<ValidationIssue>()
            .Should().Equal(new ValidationIssue(nameof(editor.Name), "FormValidation.Name.Required"));
        editor.SaveCommand.CanExecute(null).Should().BeFalse();

        editor.Name = "Walking";

        validation.HasErrors.Should().BeFalse();
        validation.GetErrors(nameof(editor.Name)).Cast<ValidationIssue>().Should().BeEmpty();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Constructors_ValidateNewAndLoadedDrafts()
    {
        var newEditor = CreateEditor();
        newEditor.HasErrors.Should().BeFalse();
        newEditor.SaveCommand.CanExecute(null).Should().BeTrue();

        var activity = new TrendActivity { Name = "x", Unit = new string('a', 17), Target = -1 };
        var existingEditor = CreateTrendEditor(activity, Mock.Of<IActivityRepository>());

        existingEditor.IsCreatingNew.Should().BeFalse();
        existingEditor.IsTargetChecked.Should().BeTrue();
        existingEditor.HasErrors.Should().BeTrue();
        existingEditor.SaveCommand.CanExecute(null).Should().BeFalse();
        existingEditor.GetErrors(nameof(existingEditor.Name)).Cast<ValidationIssue>().Should().ContainSingle();
        existingEditor.GetErrors(nameof(existingEditor.Unit)).Cast<ValidationIssue>().Should().ContainSingle();
        existingEditor.GetErrors(nameof(existingEditor.Target)).Cast<ValidationIssue>().Should().ContainSingle();
    }

    [Fact]
    public void TypeChanges_RevalidateDependentFieldsAndNotifySaveAvailability()
    {
        var editor = CreateEditor();
        var notifications = 0;
        editor.SaveCommand.CanExecuteChanged += (_, _) => notifications++;

        editor.SelectedType = ActivityType.Milestone;

        notifications.Should().BeGreaterThan(0);
        editor.SaveCommand.CanExecute(null).Should().BeFalse();
        editor.GetErrors(nameof(editor.Target)).Cast<ValidationIssue>()
            .Should().Equal(new ValidationIssue(nameof(editor.Target), "FormValidation.Target.Required"));

        editor.Unit = new string('a', 17);
        notifications = 0;
        editor.SelectedType = ActivityType.Routine;

        notifications.Should().BeGreaterThan(0);
        editor.HasErrors.Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();

        editor.SelectedType = ActivityType.Milestone;
        editor.Unit = "km";
        editor.Target = 10;

        editor.HasErrors.Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void TargetCheckbox_RevalidatesAndDisablingItClearsTheTarget()
    {
        var editor = CreateEditor();
        editor.Target = -1;
        editor.HasErrors.Should().BeFalse();

        editor.IsTargetChecked = true;
        editor.HasErrors.Should().BeTrue();
        editor.SaveCommand.CanExecute(null).Should().BeFalse();

        editor.IsTargetChecked = false;
        editor.Target.Should().BeNull();
        editor.HasErrors.Should().BeFalse();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();

        editor.IsTargetChecked = true;
        editor.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task SaveCommand_WhenInvokedDirectlyWithInvalidDraft_DoesNotCreate()
    {
        var repository = new Mock<IActivityRepository>();
        var editor = new TestActivityEditorViewModel(repository.Object);
        editor.SelectedType = ActivityType.Milestone;
        editor.ClearValidation();
        editor.SaveCommand.CanExecute(null).Should().BeTrue();

        await editor.SaveCommand.ExecuteAsync(null);

        editor.HasErrors.Should().BeTrue();
        repository.Verify(r => r.CreateActivityAsync(It.IsAny<CreateActivityRequest>()), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_WithInvalidLoadedDraft_DoesNotUpdate()
    {
        var repository = new Mock<IActivityRepository>();
        var editor = CreateTrendEditor(new TrendActivity { Name = "x" }, repository.Object);

        await editor.SaveCommand.ExecuteAsync(null);

        repository.Verify(r => r.UpdateActivityAsync(It.IsAny<Guid>(), It.IsAny<UpdateActivityRequest>()), Times.Never);
    }

    [Theory]
    [InlineData(ActivityType.Routine)]
    [InlineData(ActivityType.Trend)]
    public async Task SaveCommand_CreatesUsingOnlyActiveFields(ActivityType type)
    {
        var repository = new Mock<IActivityRepository>();
        repository.Setup(r => r.CreateActivityAsync(It.IsAny<CreateActivityRequest>())).ReturnsAsync(new TrendActivity());
        var editor = CreateEditor(repository.Object);
        editor.Name = " Walking ";
        editor.SelectedType = type;
        editor.Unit = "km";
        editor.Target = -1;
        editor.StartDate = new DateTime(2026, 10, 9);
        editor.TargetDate = new DateTime(2026, 10, 8);

        await editor.SaveCommand.ExecuteAsync(null);

        repository.Verify(r => r.CreateActivityAsync(It.Is<CreateActivityRequest>(request =>
            request.Name == " Walking " && request.Type == type && request.Target == null &&
            request.Unit == (type == ActivityType.Trend ? "km" : null) &&
            request.Aggregation == (type == ActivityType.Trend ? TrendAggregation.Average : (TrendAggregation?)null) &&
            request.StartDate == null && request.TargetDate == null)), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_UpdatesUsingOnlyActiveFields()
    {
        var repository = new Mock<IActivityRepository>();
        var activity = new TrendActivity { Name = "Walking", Target = 10 };
        var editor = CreateTrendEditor(activity, repository.Object);
        editor.IsTargetChecked = false;
        editor.Target = -1;
        editor.StartDate = new DateTime(2026, 10, 9);
        editor.TargetDate = new DateTime(2026, 10, 8);

        await editor.SaveCommand.ExecuteAsync(null);

        repository.Verify(r => r.UpdateActivityAsync(activity.Id, It.Is<UpdateActivityRequest>(request =>
            request.Name == "Walking" && request.Target == null && request.StartDate == null && request.TargetDate == null)), Times.Once);
    }

    [Fact]
    public async Task SaveCommand_PreservesMilestoneDatesWithoutAddingAnOrderingRule()
    {
        var repository = new Mock<IActivityRepository>();
        repository.Setup(r => r.CreateActivityAsync(It.IsAny<CreateActivityRequest>())).ReturnsAsync(new MilestoneActivity());
        var editor = CreateEditor(repository.Object);
        editor.SelectedType = ActivityType.Milestone;
        editor.Target = 0.5;
        editor.StartDate = new DateTime(2026, 10, 9);
        editor.TargetDate = new DateTime(2026, 10, 8);

        await editor.SaveCommand.ExecuteAsync(null);

        repository.Verify(r => r.CreateActivityAsync(It.Is<CreateActivityRequest>(request =>
            request.Target == 0.5 && request.Aggregation == null &&
            request.StartDate == new DateOnly(2026, 10, 9) && request.TargetDate == new DateOnly(2026, 10, 8))), Times.Once);
    }

    private static ActivityEditorViewModel CreateEditor(IActivityRepository? repository = null)
    {
        return new ActivityEditorViewModel(
            repository ?? Mock.Of<IActivityRepository>(), Mock.Of<IDialogService>(), Mock.Of<ILogger<ActivityEditorViewModel>>());
    }

    private static ActivityEditorViewModel CreateTrendEditor(TrendActivity activity, IActivityRepository repository)
    {
        var dialogService = Mock.Of<IDialogService>();
        var activityViewModel = new TrendActivityViewModel(
            activity, new TrendActivityDataProvider(TimeProvider.System), Mock.Of<IActivityDataEntryRepository>(),
            repository, dialogService, Mock.Of<ILogger<ActivityViewModel>>());

        return new ActivityEditorViewModel(
            new ActivityEditorService(), repository, dialogService, activityViewModel, Mock.Of<ILogger<ActivityEditorViewModel>>());
    }

    private sealed class TestActivityEditorViewModel(IActivityRepository repository)
        : ActivityEditorViewModel(repository, Mock.Of<IDialogService>(), Mock.Of<ILogger<ActivityEditorViewModel>>())
    {
        public void ClearValidation() => SetValidationErrors([]);
    }
}
