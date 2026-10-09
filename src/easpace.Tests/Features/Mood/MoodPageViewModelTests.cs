// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Mood.Contracts;
using easpace.Desktop.Features.Mood.Entities;
using easpace.Desktop.Features.Mood.Repositories;
using easpace.Desktop.Features.Mood.ViewModels;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace easpace.Tests.Features.Mood;

public class MoodPageViewModelTests
{
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void Slider_RejectsOutOfRangeValuesAndClearsSharedErrorsOnValidInput(double value)
    {
        var page = CreatePage(new Mock<IMoodEntryRepository>());
        page.MoodSliderValue.Should().Be(0.5);
        page.HasErrors.Should().BeFalse();
        var stateText = page.MoodStateText;
        var labels = page.MoodLabels.ToArray();
        var changedMembers = new List<string?>();
        page.ErrorsChanged += (_, args) => changedMembers.Add(args.PropertyName);

        page.MoodSliderValue = value;

        page.MoodSliderValue.Should().Be(0.5);
        page.MoodStateText.Should().Be(stateText);
        page.MoodLabels.Should().Equal(labels);
        page.HasErrors.Should().BeTrue();
        page.GetErrors(nameof(page.MoodSliderValue)).Cast<ValidationIssue>().Should().Equal(
            new ValidationIssue(nameof(page.MoodSliderValue), "Mood.Validation.Range"));

        page.MoodSliderValue = 0.5;

        page.HasErrors.Should().BeFalse();
        page.GetErrors(nameof(page.MoodSliderValue)).Cast<ValidationIssue>().Should().BeEmpty();
        changedMembers.Should().Equal(nameof(page.MoodSliderValue), nameof(page.MoodSliderValue));
    }

    [Fact]
    public async Task Save_RevalidatesAndSavesTheRetainedValueAfterARejectedAssignment()
    {
        var repository = new Mock<IMoodEntryRepository>();
        var page = CreatePage(repository);
        page.MoodSliderValue = 0.75;
        page.MoodSliderValue = 2;
        page.HasErrors.Should().BeTrue();
        page.SaveCommand.CanExecute(null).Should().BeTrue();
        bool? hadErrorsAtSave = null;
        repository.Setup(r => r.CreateMoodEntryAsync(It.IsAny<UpsertMoodEntryRequest>()))
            .Callback<UpsertMoodEntryRequest>(_ => hadErrorsAtSave = page.HasErrors)
            .ReturnsAsync(new MoodEntry { Value = 0.75 });

        await page.SaveCommand.ExecuteAsync(null);

        repository.Verify(r
                => r.CreateMoodEntryAsync(It.Is<UpsertMoodEntryRequest>(request =>
                    Math.Abs(request.Value - 0.75) < 0.01 && request.Description == string.Empty &&
                    request.Labels.Count == 0)), Times.Once);

        hadErrorsAtSave.Should().BeFalse();
        page.MoodEntries.Should().ContainSingle().Which.Value.Should().Be(0.75);
        page.MoodSliderValue.Should().Be(0.5);
        page.HasErrors.Should().BeFalse();
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(double.NaN)]
    public async Task Save_PreservesAcceptedValuesDescriptionAndSelectedLabels(double value)
    {
        var repository = new Mock<IMoodEntryRepository>();
        var page = CreatePage(repository);
        page.MoodSliderValue = value;
        page.Description = "  " + new string('a', 513) + "  ";
        var description = page.Description;
        var label = page.MoodLabels.First();
        label.IsChecked = true;
        UpsertMoodEntryRequest? savedRequest = null;
        repository.Setup(r => r.CreateMoodEntryAsync(It.IsAny<UpsertMoodEntryRequest>()))
            .Callback<UpsertMoodEntryRequest>(request => savedRequest = request)
            .ReturnsAsync(new MoodEntry());

        page.HasErrors.Should().BeFalse();
        await page.SaveCommand.ExecuteAsync(null);

        savedRequest.Should().NotBeNull();
        savedRequest!.Value.Equals(value).Should().BeTrue();
        savedRequest.Description.Should().Be(description);
        savedRequest.Labels.Should().Equal(label.State);
        page.Description.Should().BeEmpty();
        label.IsChecked.Should().BeFalse();
        page.MoodSliderValue.Should().Be(0.5);
        page.HasErrors.Should().BeFalse();
    }

    private static MoodPageViewModel CreatePage(Mock<IMoodEntryRepository> repository) => new(
        repository.Object, Mock.Of<IDialogService>(), NullLogger<MoodPageViewModel>.Instance);
}