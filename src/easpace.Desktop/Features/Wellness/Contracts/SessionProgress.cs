// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Wellness.Constants;

namespace easpace.Desktop.Features.Wellness.Contracts;

/// <summary>
/// Represents the current state of a running wellness session.
/// </summary>
/// <param name="TimerText">The formatted session time: remaining when the session has a fixed length, otherwise elapsed.</param>
/// <param name="InstructionText">The instruction currently shown to the user.</param>
/// <param name="StepSecondsText">The remaining seconds of the current step, or empty when the session has no steps.</param>
/// <param name="Phase">The breathing phase of the current step, if any.</param>
/// <param name="StepIndex">The zero-based index of the current step within its cycle.</param>
/// <param name="StepCount">The number of steps in a single cycle.</param>
/// <param name="CycleIndex">The zero-based index of the current cycle.</param>
/// <param name="Cycles">The number of cycles to run, or <c>null</c> when the session doesn't run in cycles.</param>
internal sealed record SessionProgress(
    string TimerText,
    string InstructionText,
    string StepSecondsText,
    BreathingPhaseType? Phase,
    int StepIndex,
    int StepCount,
    int CycleIndex,
    int? Cycles);
