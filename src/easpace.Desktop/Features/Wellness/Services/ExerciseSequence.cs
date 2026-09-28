// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Contracts;

namespace easpace.Desktop.Features.Wellness.Services;

/// <summary>
/// Tracks the position within an ordered list of exercise steps repeated over a number of cycles.
/// The sequence is advanced in whole seconds and has no dependency on timers or UI.
/// </summary>
internal sealed class ExerciseSequence
{
    private readonly ExerciseStep[] _steps;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExerciseSequence"/> class.
    /// </summary>
    /// <param name="steps">The steps of a single cycle, in execution order.</param>
    /// <param name="cycles">The number of cycles to run, or <c>null</c> to loop indefinitely.</param>
    public ExerciseSequence(IReadOnlyList<ExerciseStep> steps, int? cycles)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (steps.Count == 0)
        {
            throw new ArgumentException("A sequence requires at least one step.", nameof(steps));
        }

        if (steps.Any(s => s.DurationSeconds <= 0))
        {
            throw new ArgumentException("Every step must have a positive duration.", nameof(steps));
        }

        if (cycles < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cycles), cycles, "The number of cycles must be at least 1.");
        }

        _steps = steps.ToArray();
        Cycles = cycles;
        CycleSeconds = _steps.Sum(s => s.DurationSeconds);
    }

    public IReadOnlyList<ExerciseStep> Steps => _steps;

    /// <summary>
    /// Gets the number of cycles to run, or <c>null</c> when the sequence loops indefinitely.
    /// </summary>
    public int? Cycles { get; }

    /// <summary>
    /// Gets the total duration of a single cycle in seconds.
    /// </summary>
    public int CycleSeconds { get; }

    /// <summary>
    /// Gets the total duration of all cycles in seconds, or <c>null</c> when the sequence loops indefinitely.
    /// </summary>
    public int? TotalSeconds => Cycles * CycleSeconds;

    public int StepIndex { get; private set; }

    public int CycleIndex { get; private set; }

    public int StepElapsedSeconds { get; private set; }

    public int StepRemainingSeconds => CurrentStep.DurationSeconds - StepElapsedSeconds;

    /// <summary>
    /// Gets the position within the whole sequence in seconds.
    /// </summary>
    public int ElapsedSeconds =>
        CycleIndex * CycleSeconds + _steps.Take(StepIndex).Sum(s => s.DurationSeconds) + StepElapsedSeconds;

    public ExerciseStep CurrentStep => _steps[StepIndex];

    /// <summary>
    /// Gets the number of fully completed cycles.
    /// </summary>
    public int CompletedCycles => IsCompleted ? Cycles!.Value : CycleIndex;

    /// <summary>
    /// Gets whether the last step of the last cycle has finished. Never true when looping indefinitely.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Advances the sequence by one second.
    /// </summary>
    /// <returns><c>true</c> when the current step changed or the sequence completed; otherwise <c>false</c>.</returns>
    public bool Tick()
    {
        if (IsCompleted) return false;

        StepElapsedSeconds++;

        if (StepElapsedSeconds < CurrentStep.DurationSeconds) return false;

        AdvanceToNextStep();
        return true;
    }

    /// <summary>
    /// Jumps to the start of the next step, wrapping into the next cycle. Moving past the final step completes the sequence.
    /// </summary>
    /// <returns><c>false</c> when the sequence is already completed; otherwise <c>true</c>.</returns>
    public bool MoveNext()
    {
        if (IsCompleted) return false;

        AdvanceToNextStep();
        return true;
    }

    /// <summary>
    /// Jumps to the start of the previous step, wrapping back into the previous cycle.
    /// When the sequence is completed, it returns to the start of the final step.
    /// </summary>
    /// <returns><c>false</c> when already at the first step of the first cycle; otherwise <c>true</c>.</returns>
    public bool MovePrevious()
    {
        if (IsCompleted)
        {
            IsCompleted = false;
            StepElapsedSeconds = 0;
            return true;
        }

        if (StepIndex > 0)
        {
            StepIndex--;
        }
        else if (CycleIndex > 0)
        {
            CycleIndex--;
            StepIndex = _steps.Length - 1;
        }
        else
        {
            return false;
        }

        StepElapsedSeconds = 0;
        return true;
    }

    /// <summary>
    /// Returns to the start of the first step of the first cycle.
    /// </summary>
    public void Reset()
    {
        StepIndex = 0;
        CycleIndex = 0;
        StepElapsedSeconds = 0;
        IsCompleted = false;
    }

    private void AdvanceToNextStep()
    {
        var isLastStep = StepIndex == _steps.Length - 1;
        var isLastCycle = CycleIndex == Cycles - 1;

        if (isLastStep && isLastCycle)
        {
            // stay on the final step with no time remaining
            StepElapsedSeconds = CurrentStep.DurationSeconds;
            IsCompleted = true;
            return;
        }

        if (isLastStep)
        {
            StepIndex = 0;
            CycleIndex++;
        }
        else
        {
            StepIndex++;
        }

        StepElapsedSeconds = 0;
    }
}
