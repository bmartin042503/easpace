// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Models;

/// <summary>
/// Represents a recorded wellness session.
/// </summary>
public class WellnessSession
{
    /// <summary>
    /// Gets or sets the unique identifier for the session.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the session started.
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// Gets or sets the originally planned duration for the session.
    /// </summary>
    public TimeSpan TargetDuration { get; set; }

    /// <summary>
    /// Gets or sets the actual duration the session lasted.
    /// </summary>
    public TimeSpan ActualDuration { get; set; }

    /// <summary>
    /// Gets or sets the type of the wellness session (e.g., breathing, meditation).
    /// </summary>
    public WellnessSessionType Type { get; set; }

    /// <summary>
    /// Gets or sets the breathing technique used during the session, if applicable.
    /// </summary>
    public BreathingTechnique? BreathingTechnique { get; set; }

    /// <summary>
    /// Gets or sets the actual number of breathing cycles completed during the session.
    /// </summary>
    public int ActualBreathingCycles { get; set; }
}