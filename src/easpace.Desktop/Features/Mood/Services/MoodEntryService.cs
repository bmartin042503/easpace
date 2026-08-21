// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Mood.Contracts;
using easpace.Desktop.Features.Mood.Entities;

namespace easpace.Desktop.Features.Mood.Services;

// Temporary in-memory implementation.
public class MoodEntryService : IMoodEntryService
{
    private readonly List<MoodEntry> _moodEntries = [];

    public MoodEntry CreateMoodEntry(UpsertMoodEntryRequest upsertRequest)
    {
        var moodEntry = new MoodEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = upsertRequest.Timestamp,
            Description = upsertRequest.Description,
            Labels = upsertRequest.Labels,
            Value = upsertRequest.Value
        };

        _moodEntries.Add(moodEntry);

        return moodEntry;
    }

    public IReadOnlyList<MoodEntry> GetMoodEntries() => _moodEntries;

    public MoodEntry? UpdateMoodEntry(Guid entryId, UpsertMoodEntryRequest upsertRequest)
    {
        var existingEntry = _moodEntries.FirstOrDefault(e => e.Id == entryId);

        if (existingEntry == null) return null;

        existingEntry.Timestamp = upsertRequest.Timestamp;
        existingEntry.Description = upsertRequest.Description;
        existingEntry.Labels = upsertRequest.Labels;
        existingEntry.Value = upsertRequest.Value;

        return existingEntry;
    }

    public bool DeleteMoodEntry(Guid entryId)
    {
        var entry = _moodEntries.FirstOrDefault(entry => entry.Id == entryId);
        return entry is not null && _moodEntries.Remove(entry);
    }
}