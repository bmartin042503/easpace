// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Headless.XUnit;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Features.Wellness.ViewModels.Exercises;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness;

public class WellnessExercisePreviewViewModelTests
{
    [AvaloniaFact]
    public void LoadAndUpdate_RejectInvalidPlaybackWithoutReplacingTheLoadedExercise()
    {
        var player = new WellnessExercisePlayer();
        using var preview = new WellnessExercisePreviewViewModel(player, TimeProvider.System);
        preview.Load(new WellnessExercise
        {
            Instructions = [new WellnessExerciseInstruction { DurationSeconds = 4 }]
        }, 2).Should().BeTrue();
        var currentInstruction = preview.CurrentInstruction;
        var notifications = 0;
        player.PlaybackChanged += (_, _) => notifications++;

        (int Cycles, int Duration, BreathingPhase Phase, bool Empty)[] invalidInputs =
        [
            (0, 4, BreathingPhase.None, false),
            (-1, 4, BreathingPhase.None, false),
            (1, 0, BreathingPhase.None, false),
            (1, -1, BreathingPhase.None, false),
            (1, 4, BreathingPhase.None, true),
            (1, 4, (BreathingPhase)999, false)
        ];

        foreach (var input in invalidInputs)
        {
            var exercise = new WellnessExercise
            {
                Instructions = input.Empty ? [] :
                [
                    new WellnessExerciseInstruction { DurationSeconds = input.Duration, BreathingPhase = input.Phase }
                ]
            };
            preview.Load(exercise, input.Cycles).Should().BeFalse();
            preview.UpdateExercise(new UpsertWellnessExerciseRequest("", "", input.Cycles,
                input.Empty ? [] : [new UpsertWellnessExerciseInstructionRequest("", input.Duration, input.Phase)]))
                .Should().BeFalse();

            preview.CurrentInstruction.Should().BeSameAs(currentInstruction);
            preview.TotalCycles.Should().Be(2);
            preview.State.Should().Be(WellnessExercisePlaybackState.Paused);
            preview.InstructionElapsed.Should().Be(TimeSpan.Zero);
        }

        notifications.Should().Be(0);

        // Restart must still use the accepted snapshot after rejected loads and updates.
        preview.StopCommand.Execute(null);
        preview.CurrentInstruction.Should().BeNull();
        preview.RestartCommand.Execute(null);
        preview.TotalCycles.Should().Be(2);
        preview.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(4));
        preview.State.Should().Be(WellnessExercisePlaybackState.Paused);

        preview.UpdateExercise(new UpsertWellnessExerciseRequest("", "", 3,
            [new UpsertWellnessExerciseInstructionRequest("", 5, BreathingPhase.None)])).Should().BeTrue();
        preview.TotalCycles.Should().Be(3);
        preview.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(5));
        preview.CurrentInstruction!.Text.Should().BeEmpty();
        preview.CurrentInstruction.BreathingPhase.Should().Be(BreathingPhase.None);
    }

    [AvaloniaFact]
    public void InstructionTimeText_ShowsElapsedAndTotalTimeOfTheCurrentInstruction()
    {
        var player = new WellnessExercisePlayer();
        using var preview = new WellnessExercisePreviewViewModel(player, TimeProvider.System);
        preview.InstructionTimeText.Should().BeEmpty();

        preview.Load(new WellnessExercise
        {
            Instructions = [new WellnessExerciseInstruction { DurationSeconds = 30 }]
        }, 1);
        preview.PlayCommand.Execute(null);
        player.Advance(TimeSpan.FromSeconds(7.6));

        preview.InstructionTimeText.Should().Be("00:07 / 00:30");
    }
}
