// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Services;
using FluentAssertions;

namespace easpace.Tests.Features.Wellness;

public class WellnessExercisePlayerTests
{
    [Fact]
    public void Start_OrdersAndCopiesInstructionsAndInitializesPlayback()
    {
        var exercise = CreateExercise();
        var firstInstruction = exercise.Instructions.Single(instruction => instruction.Order == 0);
        var player = new WellnessExercisePlayer();

        player.State.Should().Be(WellnessExercisePlaybackState.Idle);
        player.CurrentInstruction.Should().BeNull();
        player.CurrentInstructionIndex.Should().Be(-1);

        player.Start(exercise, 2);

        player.State.Should().Be(WellnessExercisePlaybackState.Playing);
        player.CurrentCycle.Should().Be(1);
        player.TotalCycles.Should().Be(2);
        player.CurrentInstructionIndex.Should().Be(0);
        player.CurrentInstruction.Should().BeEquivalentTo(firstInstruction);
        player.CurrentInstruction.Should().NotBeSameAs(firstInstruction);
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(4));

        firstInstruction.Text = "Changed";
        firstInstruction.DurationSeconds = 20;
        exercise.Instructions.Clear();

        player.CurrentInstruction.Text.Should().Be("Breathe in");
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(4));

        player.Next();

        player.CurrentInstruction.Text.Should().Be("Breathe out");
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(6));
    }

    [Theory]
    [InlineData(0, 1, 0, 0, 4, false)]
    [InlineData(2.5, 1, 0, 2.5, 1.5, false)]
    [InlineData(4, 1, 1, 0, 6, false)]
    [InlineData(10, 2, 0, 0, 4, false)]
    [InlineData(12.5, 2, 0, 2.5, 1.5, false)]
    [InlineData(20, 2, 1, 6, 0, true)]
    [InlineData(25, 2, 1, 6, 0, true)]
    public void Advance_TracksTimeAcrossInstructionsAndCycles(
        double seconds,
        int expectedCycle,
        int expectedIndex,
        double expectedElapsed,
        double expectedRemaining,
        bool isCompleted)
    {
        var player = new WellnessExercisePlayer();
        player.Start(CreateExercise(), 2);

        player.Advance(TimeSpan.FromSeconds(seconds));

        var expectedState = isCompleted
            ? WellnessExercisePlaybackState.Completed
            : WellnessExercisePlaybackState.Playing;

        player.State.Should().Be(expectedState);
        player.CurrentCycle.Should().Be(expectedCycle);
        player.CurrentInstructionIndex.Should().Be(expectedIndex);
        player.CurrentInstruction!.Order.Should().Be(expectedIndex);
        player.InstructionElapsed.Should().Be(TimeSpan.FromSeconds(expectedElapsed));
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(expectedRemaining));
    }

    [Fact]
    public void PauseAndResume_PreservePositionAndIgnoreTimeWhilePaused()
    {
        var player = new WellnessExercisePlayer();
        player.Start(CreateExercise(), 2);
        player.Advance(TimeSpan.FromSeconds(2));

        player.Pause();
        player.Advance(TimeSpan.FromSeconds(30));

        player.State.Should().Be(WellnessExercisePlaybackState.Paused);
        player.CurrentCycle.Should().Be(1);
        player.CurrentInstructionIndex.Should().Be(0);
        player.InstructionElapsed.Should().Be(TimeSpan.FromSeconds(2));
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(2));

        player.Resume();
        player.Advance(TimeSpan.FromSeconds(3));

        player.State.Should().Be(WellnessExercisePlaybackState.Playing);
        player.CurrentCycle.Should().Be(1);
        player.CurrentInstructionIndex.Should().Be(1);
        player.InstructionElapsed.Should().Be(TimeSpan.FromSeconds(1));
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NextAndPrevious_CrossCycleBoundariesAndPreservePlaybackState(bool isPaused)
    {
        var player = new WellnessExercisePlayer();
        player.Start(CreateExercise(), 2);
        player.Advance(TimeSpan.FromSeconds(2));

        if (isPaused) player.Pause();

        var expectedState = isPaused
            ? WellnessExercisePlaybackState.Paused
            : WellnessExercisePlaybackState.Playing;

        player.Next();

        player.CurrentCycle.Should().Be(1);
        player.CurrentInstructionIndex.Should().Be(1);
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(6));
        player.State.Should().Be(expectedState);

        // move from the final instruction to the first instruction of the next cycle
        player.Next();

        player.CurrentCycle.Should().Be(2);
        player.CurrentInstructionIndex.Should().Be(0);
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(4));
        player.State.Should().Be(expectedState);

        player.Advance(TimeSpan.FromSeconds(1));

        // return to the final instruction of the previous cycle
        player.Previous();

        player.CurrentCycle.Should().Be(1);
        player.CurrentInstructionIndex.Should().Be(1);
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(6));
        player.State.Should().Be(expectedState);

        player.Previous();

        player.CurrentCycle.Should().Be(1);
        player.CurrentInstructionIndex.Should().Be(0);
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(4));

        // navigating backwards cannot move before the first instruction
        player.Previous();

        player.CurrentCycle.Should().Be(1);
        player.CurrentInstructionIndex.Should().Be(0);
        player.State.Should().Be(expectedState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompletedPlayback_PreservesTheFinalInstructionAndAllowsReturningToIt(bool useNext)
    {
        var player = new WellnessExercisePlayer();
        player.Start(CreateExercise(), 2);

        if (useNext)
        {
            for (var i = 0; i < 4; i++) player.Next();
        }
        else
        {
            player.Advance(TimeSpan.FromSeconds(20));
        }

        player.State.Should().Be(WellnessExercisePlaybackState.Completed);
        player.CurrentCycle.Should().Be(2);
        player.CurrentInstructionIndex.Should().Be(1);
        player.CurrentInstruction!.Text.Should().Be("Breathe out");
        player.InstructionElapsed.Should().Be(TimeSpan.FromSeconds(6));
        player.InstructionRemaining.Should().Be(TimeSpan.Zero);

        player.Resume();
        player.Next();
        player.Advance(TimeSpan.FromSeconds(10));

        player.State.Should().Be(WellnessExercisePlaybackState.Completed);
        player.CurrentCycle.Should().Be(2);
        player.CurrentInstructionIndex.Should().Be(1);
        player.InstructionElapsed.Should().Be(TimeSpan.FromSeconds(6));

        player.Previous();

        player.State.Should().Be(WellnessExercisePlaybackState.Paused);
        player.CurrentCycle.Should().Be(2);
        player.CurrentInstructionIndex.Should().Be(1);
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(6));

        player.Resume();
        player.Advance(TimeSpan.FromSeconds(6));

        player.State.Should().Be(WellnessExercisePlaybackState.Completed);
    }

    [Fact]
    public void Stop_ClearsPlaybackAndAllowsStartingAgain()
    {
        var player = new WellnessExercisePlayer();
        player.Start(CreateExercise(), 2);
        player.Advance(TimeSpan.FromSeconds(12));

        player.Stop();
        player.Resume();
        player.Next();
        player.Previous();
        player.Advance(TimeSpan.FromSeconds(10));

        player.State.Should().Be(WellnessExercisePlaybackState.Stopped);
        player.CurrentCycle.Should().Be(0);
        player.TotalCycles.Should().Be(0);
        player.CurrentInstructionIndex.Should().Be(-1);
        player.CurrentInstruction.Should().BeNull();
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.Zero);

        player.Start(CreateExercise(), 3);

        player.State.Should().Be(WellnessExercisePlaybackState.Playing);
        player.CurrentCycle.Should().Be(1);
        player.TotalCycles.Should().Be(3);
        player.CurrentInstructionIndex.Should().Be(0);
        player.InstructionElapsed.Should().Be(TimeSpan.Zero);
        player.InstructionRemaining.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void Advance_RaisesPlaybackChangedOnceWithTheUpdatedPosition()
    {
        var player = new WellnessExercisePlayer();
        player.Start(CreateExercise(), 2);

        var notificationCount = 0;
        var observedCycle = 0;
        var observedIndex = -1;
        var observedElapsed = TimeSpan.Zero;

        player.PlaybackChanged += (_, _) =>
        {
            notificationCount++;
            observedCycle = player.CurrentCycle;
            observedIndex = player.CurrentInstructionIndex;
            observedElapsed = player.InstructionElapsed;
        };

        player.Advance(TimeSpan.FromSeconds(15));

        notificationCount.Should().Be(1);
        observedCycle.Should().Be(2);
        observedIndex.Should().Be(1);
        observedElapsed.Should().Be(TimeSpan.FromSeconds(1));
    }

    private static WellnessExercise CreateExercise()
    {
        return new WellnessExercise
        {
            Name = "Test exercise",
            Instructions =
            [
                // deliberately unsorted to verify playback order
                new WellnessExerciseInstruction
                {
                    Order = 1,
                    Text = "Breathe out",
                    DurationSeconds = 6,
                    BreathingPhase = BreathingPhase.Exhale
                },
                new WellnessExerciseInstruction
                {
                    Order = 0,
                    Text = "Breathe in",
                    DurationSeconds = 4,
                    BreathingPhase = BreathingPhase.Inhale
                }
            ]
        };
    }
}