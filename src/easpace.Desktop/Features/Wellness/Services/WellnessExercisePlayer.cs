// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using easpace.Desktop.Features.Wellness.Validation;

namespace easpace.Desktop.Features.Wellness.Services;

/// <inheritdoc/>
internal class WellnessExercisePlayer : IWellnessExercisePlayer
{
    private WellnessExerciseInstruction[] _instructions = [];

    public WellnessExercisePlaybackState State { get; private set; } = WellnessExercisePlaybackState.Idle;
    public int CurrentCycle { get; private set; }
    public int TotalCycles { get; private set; }
    public int CurrentInstructionIndex { get; private set; } = -1;

    public WellnessExerciseInstruction? CurrentInstruction =>
        CurrentInstructionIndex >= 0 ? _instructions[CurrentInstructionIndex] : null;

    public TimeSpan InstructionElapsed { get; private set; }

    public TimeSpan InstructionRemaining => CurrentInstruction is { } instruction
        ? TimeSpan.FromSeconds(instruction.DurationSeconds) - InstructionElapsed
        : TimeSpan.Zero;

    /// <inheritdoc/>
    public event EventHandler? PlaybackChanged;

    /// <inheritdoc/>
    public void UpdateExercise(UpsertWellnessExerciseRequest updateRequest)
    {
        var currentInstruction = CurrentInstruction;

        if (currentInstruction == null) return;

        // the request's list order defines the playback positions
        var instructions = updateRequest.Instructions
            .Select((instruction, index) => new WellnessExerciseInstruction
            {
                ExerciseId = currentInstruction.ExerciseId,
                Order = index,
                Text = instruction.Text,
                DurationSeconds = instruction.DurationSeconds,
                BreathingPhase = instruction.BreathingPhase
            })
            .ToArray();

        if (!WellnessPlaybackValidator.IsPlayable(instructions, updateRequest.DefaultCycleCount, validateBreathingPhases: true))
        {
            return;
        }

        var instructionIndex = Math.Min(CurrentInstructionIndex, instructions.Length - 1);
        var instruction = instructions[instructionIndex];
        var instructionDuration = TimeSpan.FromSeconds(instruction.DurationSeconds);
        var instructionElapsed = InstructionElapsed;
        var currentCycle = Math.Min(CurrentCycle, updateRequest.DefaultCycleCount);
        var state = State;

        if (instructionIndex != CurrentInstructionIndex ||
            instruction.BreathingPhase != currentInstruction.BreathingPhase)
        {
            instructionElapsed = TimeSpan.Zero;
        }

        if (instructionElapsed > instructionDuration)
        {
            instructionElapsed = instructionDuration;
        }

        // keep the position instead of jumping to the new end of an extended exercise
        if (state == WellnessExercisePlaybackState.Completed
            && (currentCycle < updateRequest.DefaultCycleCount
                || instructionIndex < instructions.Length - 1
                || instructionElapsed < instructionDuration))
        {
            state = WellnessExercisePlaybackState.Paused;
        }

        // replace the definition only after validation and position adjustment succeed
        _instructions = instructions;
        TotalCycles = updateRequest.DefaultCycleCount;
        CurrentCycle = currentCycle;
        CurrentInstructionIndex = instructionIndex;
        InstructionElapsed = instructionElapsed;
        State = state;

        OnPlaybackChanged();
    }

    /// <inheritdoc/>
    public void Start(WellnessExercise exercise, int cycleCount)
    {
        if (!WellnessPlaybackValidator.IsPlayable(exercise.Instructions, cycleCount, validateBreathingPhases: false)) return;

        // copy the instructions
        var instructions = exercise.Instructions
            .OrderBy(instruction => instruction.Order)
            .Select(instruction => new WellnessExerciseInstruction
            {
                Id = instruction.Id,
                ExerciseId = instruction.ExerciseId,
                Order = instruction.Order,
                Text = instruction.Text,
                DurationSeconds = instruction.DurationSeconds,
                BreathingPhase = instruction.BreathingPhase
            })
            .ToArray();

        // replace the current playback only after the new instructions have been validated
        _instructions = instructions;
        TotalCycles = cycleCount;
        CurrentCycle = 1;
        CurrentInstructionIndex = 0;
        InstructionElapsed = TimeSpan.Zero;
        State = WellnessExercisePlaybackState.Playing;

        OnPlaybackChanged();
    }

    /// <inheritdoc/>
    public void Pause()
    {
        if (State != WellnessExercisePlaybackState.Playing) return;

        State = WellnessExercisePlaybackState.Paused;

        OnPlaybackChanged();
    }

    /// <inheritdoc/>
    public void Resume()
    {
        if (State != WellnessExercisePlaybackState.Paused) return;

        State = WellnessExercisePlaybackState.Playing;

        OnPlaybackChanged();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (State is WellnessExercisePlaybackState.Idle or WellnessExercisePlaybackState.Stopped) return;

        _instructions = [];
        CurrentCycle = 0;
        TotalCycles = 0;
        CurrentInstructionIndex = -1;
        InstructionElapsed = TimeSpan.Zero;
        State = WellnessExercisePlaybackState.Stopped;

        OnPlaybackChanged();
    }

    /// <inheritdoc/>
    public void Next()
    {
        if (State is not (WellnessExercisePlaybackState.Playing or WellnessExercisePlaybackState.Paused)) return;

        MoveNextInstruction();

        OnPlaybackChanged();
    }

    /// <inheritdoc/>
    public void Previous()
    {
        if (State is not (WellnessExercisePlaybackState.Playing
            or WellnessExercisePlaybackState.Paused
            or WellnessExercisePlaybackState.Completed)) return;

        if (State == WellnessExercisePlaybackState.Completed)
        {
            State = WellnessExercisePlaybackState.Paused;
        }
        else if (CurrentInstructionIndex > 0)
        {
            CurrentInstructionIndex--;
        }
        else if (CurrentCycle > 1)
        {
            CurrentCycle--;
            CurrentInstructionIndex = _instructions.Length - 1;
        }
        else if (InstructionElapsed == TimeSpan.Zero)
        {
            return;
        }

        InstructionElapsed = TimeSpan.Zero;

        OnPlaybackChanged();
    }

    /// <inheritdoc/>
    public void Advance(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed time cannot be negative.");
        }

        if (State != WellnessExercisePlaybackState.Playing || elapsed == TimeSpan.Zero) return;

        while (elapsed > TimeSpan.Zero)
        {
            var remaining = InstructionRemaining;

            if (elapsed < remaining)
            {
                InstructionElapsed += elapsed;
                break;
            }

            // preserve excess time when a delayed tick passes an instruction boundary
            elapsed -= remaining;

            if (!MoveNextInstruction()) break;
        }

        // notify once after the entire update so observers receive a consistent position
        OnPlaybackChanged();
    }

    /// <summary>
    /// Moves to the next instruction or marks the final instruction as completed.
    /// </summary>
    private bool MoveNextInstruction()
    {
        if (CurrentInstructionIndex < _instructions.Length - 1)
        {
            CurrentInstructionIndex++;
        }
        else if (CurrentCycle < TotalCycles)
        {
            CurrentCycle++;
            CurrentInstructionIndex = 0;
        }
        else
        {
            // retain the final instruction and its completed timing for the preview
            InstructionElapsed = TimeSpan.FromSeconds(_instructions[CurrentInstructionIndex].DurationSeconds);
            State = WellnessExercisePlaybackState.Completed;
            return false;
        }

        InstructionElapsed = TimeSpan.Zero;
        return true;
    }

    private void OnPlaybackChanged()
    {
        PlaybackChanged?.Invoke(this, EventArgs.Empty);
    }
}