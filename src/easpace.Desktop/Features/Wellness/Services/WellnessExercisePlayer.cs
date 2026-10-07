// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

// TODO: implement missing functionality
internal class WellnessExercisePlayer : IWellnessExercisePlayer
{
    public int CurrentCycle { get; }
    public int TotalCycles { get; }
    public int CurrentInstructionIndex { get; }
    public WellnessExerciseInstruction CurrentInstruction { get; }
    public TimeSpan InstructionElapsed { get; }
    public TimeSpan InstructionRemaining { get; }
    public void Start(int cycleCount)
    {
        throw new NotImplementedException();
    }

    public void Pause()
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }

    public void Next()
    {
        throw new NotImplementedException();
    }

    public void Previous()
    {
        throw new NotImplementedException();
    }

    public void Advance(TimeSpan elapsed)
    {
        throw new NotImplementedException();
    }
}