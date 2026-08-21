// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

// Temporary in-memory implementation.
public class WellnessSessionEntryService : IWellnessSessionEntryService
{
    private readonly List<WellnessSessionEntry> _wellnessSessions = [];

    public WellnessSessionEntry CreateWellnessSessionEntry(CreateWellnessSessionEntryRequest createEntryRequest)
    {
        var wellnessSession = new WellnessSessionEntry
        {
            Id = Guid.NewGuid(),
            StartDate = createEntryRequest.StartDate,
            Type = createEntryRequest.SessionType,
            TargetDuration = createEntryRequest.TargetDuration,
            ActualDuration = createEntryRequest.ActualDuration,
            BreathingTechnique = createEntryRequest.BreathingTechnique
        };

        _wellnessSessions.Add(wellnessSession);

        return wellnessSession;
    }

    public IReadOnlyList<WellnessSessionEntry> GetWellnessSessionEntries() =>
        _wellnessSessions.OrderByDescending(o => o.StartDate).ToList();

    public bool DeleteWellnessSessionEntry(Guid entryId)
    {
        var session = _wellnessSessions.FirstOrDefault(s => s.Id == entryId);
        return session is not null && _wellnessSessions.Remove(session);
    }
}