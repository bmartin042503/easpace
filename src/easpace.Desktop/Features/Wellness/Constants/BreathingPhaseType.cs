// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Features.Wellness.Constants;

/// <summary>
/// Defines the various phases involved in a breathing technique.
/// </summary>
internal enum BreathingPhaseType
{
    /// <summary>
    /// The phase where breath is drawn into the lungs.
    /// </summary>
    Inhale = 0,

    /// <summary>
    /// The phase where breath is held inside the lungs after inhaling.
    /// </summary>
    HoldIn = 1,

    /// <summary>
    /// The phase where breath is released from the lungs.
    /// </summary>
    Exhale = 2,

    /// <summary>
    /// The phase where the lungs are kept empty after exhaling.
    /// </summary>
    HoldOut = 3
}