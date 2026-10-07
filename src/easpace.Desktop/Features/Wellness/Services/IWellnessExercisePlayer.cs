// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

internal interface IWellnessExercisePlayer
{
    int CurrentCycle { get; }
    int TotalCycles { get; }
    int CurrentInstructionIndex { get; }
    WellnessExerciseInstruction CurrentInstruction { get; }
    TimeSpan InstructionElapsed { get; }
    TimeSpan InstructionRemaining { get; }
    void Start(int cycleCount);
    void Pause();
    void Resume();
    void Stop();
    void Next();
    void Previous();
    void Advance(TimeSpan elapsed);
}