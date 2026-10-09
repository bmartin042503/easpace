// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Headless.XUnit;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Features.Wellness.ViewModels;
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

        (int Cycles, int Duration, BreathingPhase? Phase, bool Empty)[] invalidInputs =
        [
            (0, 4, null, false),
            (-1, 4, null, false),
            (1, 0, null, false),
            (1, -1, null, false),
            (1, 4, null, true),
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
        preview.UpdateExercise(new UpsertWellnessExerciseRequest("", "", 3,
            [new UpsertWellnessExerciseInstructionRequest("", 5, null)])).Should().BeTrue();
        preview.TotalCycles.Should().Be(3);
        preview.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(5));
        preview.CurrentInstruction!.Text.Should().BeEmpty();
        preview.CurrentInstruction.BreathingPhase.Should().BeNull();
    }
}
