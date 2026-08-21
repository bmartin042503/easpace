// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Features.Wellness.Contracts;

public sealed record SessionTexts(
    string TimerText,    
    string InstructionText,
    string? PhaseSecondsText
);