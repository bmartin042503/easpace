// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Features.Wellness.ViewModels;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Wellness;

public class WellnessStartViewModelTests
{
    private readonly Mock<IWellnessSessionEntryService> _sessionEntryServiceMock = new();
    private readonly Mock<IWellnessExerciseService> _exerciseServiceMock = new();

    public WellnessStartViewModelTests()
    {
        _sessionEntryServiceMock.Setup(s => s.GetWellnessSessionEntriesAsync()).ReturnsAsync([]);
    }

    private async Task<WellnessStartViewModel> CreateInitializedViewModelAsync(params WellnessExercise[] exercises)
    {
        _exerciseServiceMock
            .Setup(s => s.GetExercisesAsync(It.IsAny<WellnessSessionType?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercises);

        var viewModel = new WellnessStartViewModel(
            _sessionEntryServiceMock.Object,
            _exerciseServiceMock.Object,
            new Mock<IDialogService>().Object,
            new Mock<ILogger<WellnessStartViewModel>>().Object);

        await viewModel.InitializeAsync();
        return viewModel;
    }

    private static BreathingExercise Breathing(string name, bool isRepeating, params int[] durations) => new()
    {
        Name = name,
        IsRepeating = isRepeating,
        Instructions = durations
            .Select((duration, index) => new ExerciseInstruction
            {
                Order = index + 1, DurationSeconds = duration, Phase = BreathingPhaseType.Inhale
            })
            .ToList()
    };

    private static MeditationExercise Meditation(string name, bool isRepeating, params int[] durations) => new()
    {
        Name = name,
        IsRepeating = isRepeating,
        Instructions = durations
            .Select((duration, index) => new ExerciseInstruction { Order = index + 1, DurationSeconds = duration, Text = "Sit" })
            .ToList()
    };

    private static string ApproxDuration(string duration) =>
        string.Format(LocalizationService.GetString("Wellness.Label.ApproxDuration"), duration);

    private static WellnessSessionConfiguration? Start(WellnessStartViewModel viewModel)
    {
        WellnessSessionConfiguration? configuration = null;
        viewModel.SessionStarted += (_, started) => configuration = started;
        viewModel.StartSessionCommand.Execute(null);
        return configuration;
    }

    [Theory]
    [InlineData(16, 19, 225, "05:04")]
    [InlineData(19, 16, 189, "05:04")]
    [InlineData(60, 5, 60, "05:00")]
    [InlineData(360, 1, 10, "06:00")]
    [InlineData(3000, 1, 1, "50:00")]
    public async Task RepeatingExercise_DefaultsToAboutFiveMinutesWithinTheHourCap(
        int cycleSeconds, int expectedCycles, int expectedMaximumCycles, string expectedDuration)
    {
        var viewModel = await CreateInitializedViewModelAsync(Breathing("Rhythm", true, cycleSeconds));

        viewModel.ShowCycleSelector.Should().BeTrue();
        viewModel.SelectedCycles.Should().Be(expectedCycles);
        viewModel.MaximumCycles.Should().Be(expectedMaximumCycles);
        viewModel.DurationText.Should().Be(ApproxDuration(expectedDuration));
    }

    [Fact]
    public async Task SelectedCycles_UpdatesTheApproximateDurationUpToOneHour()
    {
        var viewModel = await CreateInitializedViewModelAsync(Breathing("Box", true, 4, 4, 4, 4));

        viewModel.SelectedCycles = 3;
        viewModel.DurationText.Should().Be(ApproxDuration("00:48"));

        viewModel.SelectedCycles = viewModel.MaximumCycles;
        viewModel.DurationText.Should().Be(ApproxDuration("01:00:00"));
    }

    [Fact]
    public async Task NonRepeatingExercise_HidesCycleSelectorAndShowsFixedDuration()
    {
        var viewModel = await CreateInitializedViewModelAsync(Meditation("Once", false, 4, 5));
        viewModel.SelectedSessionType = WellnessSessionType.Meditation;

        viewModel.ShowCycleSelector.Should().BeFalse();
        viewModel.DurationText.Should().Be($"{LocalizationService.GetString("Wellness.Label.Duration")}: 00:09");
    }

    [Fact]
    public async Task SelectedSessionType_ListsOnlyThatTypeAndSelectsTheFirst()
    {
        var viewModel = await CreateInitializedViewModelAsync(
            Breathing("Box", true, 4), Meditation("Calm", true, 60), Breathing("Triangle", true, 3));

        viewModel.Exercises.Select(e => e.Name).Should().Equal("Box", "Triangle");
        viewModel.SelectedExercise!.Name.Should().Be("Box");

        viewModel.SelectedSessionType = WellnessSessionType.Meditation;

        viewModel.Exercises.Select(e => e.Name).Should().Equal("Calm");
        viewModel.SelectedExercise!.Name.Should().Be("Calm");
    }

    [Fact]
    public async Task StartSession_WithRepeatingExercise_UsesTheSelectedCycles()
    {
        var exercise = Breathing("Box", true, 4, 4, 4, 4);
        var viewModel = await CreateInitializedViewModelAsync(exercise);
        viewModel.SelectedCycles = 3;

        var configuration = Start(viewModel);

        configuration.Should().NotBeNull();
        configuration!.SessionType.Should().Be(WellnessSessionType.Breathing);
        configuration.ExerciseId.Should().Be(exercise.Id);
        configuration.ExerciseName.Should().Be("Box");
        configuration.IsRepeating.Should().BeTrue();
        configuration.Steps.Should().Equal(Enumerable.Repeat(ExerciseStep.Create(null, 4, BreathingPhaseType.Inhale), 4));
        configuration.Cycles.Should().Be(3);
        configuration.TargetDuration.Should().Be(TimeSpan.FromSeconds(48));
    }

    [Fact]
    public async Task StartSession_WithNonRepeatingExercise_RunsOnce()
    {
        var viewModel = await CreateInitializedViewModelAsync(Breathing("Once", false, 4, 5));
        viewModel.SelectedCycles = 7;

        var configuration = Start(viewModel);

        configuration!.Cycles.Should().Be(1);
        configuration.IsRepeating.Should().BeFalse();
        configuration.TargetDuration.Should().Be(TimeSpan.FromSeconds(9));
    }

    [Fact]
    public async Task StartSession_NeverExceedsTheHourCap()
    {
        var viewModel = await CreateInitializedViewModelAsync(Breathing("Box", true, 4, 4, 4, 4));
        viewModel.SelectedCycles = 10_000;

        var configuration = Start(viewModel);

        configuration!.Cycles.Should().Be(viewModel.MaximumCycles);
        configuration.TargetDuration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task StartSessionCommand_WithoutSelection_CannotExecute()
    {
        var viewModel = await CreateInitializedViewModelAsync();

        viewModel.SelectedExercise.Should().BeNull();
        viewModel.StartSessionCommand.CanExecute(null).Should().BeFalse();
        Start(viewModel).Should().BeNull();
    }

    [Fact]
    public async Task StartSessionCommand_RequiresRunnableStepsAndAtLeastOneCycle()
    {
        var viewModel = await CreateInitializedViewModelAsync(Breathing("Empty", true), Breathing("Box", true, 4));

        viewModel.SelectedExercise!.Name.Should().Be("Empty");
        viewModel.StartSessionCommand.CanExecute(null).Should().BeFalse();

        viewModel.SelectedExercise = viewModel.Exercises.Single(e => e.Name == "Box");
        viewModel.StartSessionCommand.CanExecute(null).Should().BeTrue();

        viewModel.SelectedCycles = 0;
        viewModel.StartSessionCommand.CanExecute(null).Should().BeFalse();
        Start(viewModel).Should().BeNull();
    }
}
