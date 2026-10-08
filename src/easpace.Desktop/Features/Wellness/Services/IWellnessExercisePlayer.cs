// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

/// <summary>
/// Tracks exercise playback and instruction timing using externally supplied elapsed time.
/// </summary>
internal interface IWellnessExercisePlayer
{
    WellnessExercisePlaybackState State { get; }
    int CurrentCycle { get; }
    int TotalCycles { get; }
    int CurrentInstructionIndex { get; }
    WellnessExerciseInstruction? CurrentInstruction { get; }
    TimeSpan InstructionElapsed { get; }
    TimeSpan InstructionRemaining { get; }

    /// <summary>
    /// Occurs after the playback state, position, or instruction timing changes.
    /// </summary>
    event EventHandler? PlaybackChanged;

    /// <summary>
    /// Updates the loaded exercise while preserving its current instruction position where possible.
    /// </summary>
    void UpdateExercise(UpsertWellnessExerciseRequest updateRequest);

    /// <summary>
    /// Starts playback from the first instruction.
    /// </summary>
    void Start(WellnessExercise exercise, int cycleCount);
    
    /// <summary>
    /// Pauses playback without changing the current position.
    /// </summary>
    void Pause();
    
    /// <summary>
    /// Resumes paused playback from the current position.
    /// </summary>
    void Resume();
    
    /// <summary>
    /// Stops playback and clears the loaded instructions and timing.
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Moves to the next instruction, preserving the playing or paused state.
    /// Completes playback when there are no further instructions or cycles.
    /// </summary>
    void Next();
    
    /// <summary>
    /// Moves to the previous instruction, crossing cycle boundaries when necessary.
    /// After completion, returns to the beginning of the final instruction in a paused state.
    /// </summary>
    void Previous();
    
    /// <summary>
    /// Advances active playback by the supplied elapsed time.
    /// Carries remaining time across instruction and cycle boundaries.
    /// </summary>
    void Advance(TimeSpan elapsed);
}