// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Constants;

/// <summary>
/// Defines the various phases involved in a breathing technique.
/// </summary>
public enum BreathingPhaseType
{
    /// <summary>
    /// The phase where breath is drawn into the lungs.
    /// </summary>
    Inhale,

    /// <summary>
    /// The phase where breath is held inside the lungs after inhaling.
    /// </summary>
    HoldIn,

    /// <summary>
    /// The phase where breath is released from the lungs.
    /// </summary>
    Exhale,

    /// <summary>
    /// The phase where the lungs are kept empty after exhaling.
    /// </summary>
    HoldOut
}