// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.ViewModels;
using easpace.Desktop.Services.Core;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness;

public class WellnessSessionEntryViewModelTests
{
    private static WellnessSessionEntry Entry(
        int actualSeconds,
        string? exerciseName = null,
        WellnessExercise? exercise = null) => new()
    {
        Id = Guid.NewGuid(),
        StartDate = new DateTimeOffset(2026, 9, 29, 8, 0, 0, TimeSpan.Zero),
        ActualDuration = TimeSpan.FromSeconds(actualSeconds),
        ExerciseId = exercise?.Id,
        Exercise = exercise,
        ExerciseName = exerciseName
    };

    private static WellnessExercise Meditation(string name, bool isRepeating, int instructionCount) => new()
    {
        Name = name,
        IsRepeating = isRepeating,
        Instructions = Enumerable.Range(1, instructionCount)
            .Select(order => new ExerciseInstruction { Order = order, DurationSeconds = 60, Text = "Sit" })
            .ToList()
    };

    [Fact]
    public void DetailsText_WithExistingRepeatingExercise_ShowsNameDurationAndCycles()
    {
        var exercise = new WellnessExercise
        {
            Name = "Box Breathing",
            IsRepeating = true,
            Instructions =
            [
                new ExerciseInstruction { Order = 1, DurationSeconds = 4, Phase = BreathingPhaseType.Inhale },
                new ExerciseInstruction { Order = 2, DurationSeconds = 4, Phase = BreathingPhaseType.HoldIn },
                new ExerciseInstruction { Order = 3, DurationSeconds = 4, Phase = BreathingPhaseType.Exhale },
                new ExerciseInstruction { Order = 4, DurationSeconds = 4, Phase = BreathingPhaseType.HoldOut }
            ]
        };

        var entryVm = new WellnessSessionEntryViewModel(Entry(304, exercise.Name, exercise));

        var cyclesText = string.Format(LocalizationService.GetString("Wellness.Session.Cycles"), 19);
        entryVm.DetailsText.Should().Be($"Box Breathing • 05:04 • {cyclesText}");
    }

    [Fact]
    public void DetailsText_WithDeletedExercise_ShowsSavedNameAndDurationWithoutCycles()
    {
        var entryVm = new WellnessSessionEntryViewModel(Entry(125, "Calm Breath"));

        entryVm.DetailsText.Should().Be("Calm Breath • 02:05");
    }

    [Fact]
    public void DetailsText_WithoutExerciseName_ShowsDurationOnly()
    {
        var entryVm = new WellnessSessionEntryViewModel(Entry(600));

        entryVm.DetailsText.Should().Be("10:00");
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    public void DetailsText_WithoutRepeatingInstructions_ShowsNoCycles(bool isRepeating, int instructionCount)
    {
        var exercise = Meditation("Quiet Mind", isRepeating, instructionCount);

        var entryVm = new WellnessSessionEntryViewModel(Entry(120, exercise.Name, exercise));

        entryVm.DetailsText.Should().Be("Quiet Mind • 02:00");
    }

    [Theory]
    [InlineData("Saved name", "Saved name")]
    [InlineData(null, "Current name")]
    public void DetailsText_PrefersSavedNameOverExerciseName(string? savedName, string expectedName)
    {
        var exercise = Meditation("Current name", isRepeating: false, instructionCount: 1);

        var entryVm = new WellnessSessionEntryViewModel(Entry(60, savedName, exercise));

        entryVm.DetailsText.Should().Be($"{expectedName} • 01:00");
    }
}
