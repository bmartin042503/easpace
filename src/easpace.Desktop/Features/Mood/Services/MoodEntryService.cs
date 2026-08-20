// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Mood.Contracts;
using easpace.Desktop.Features.Mood.Entities;

namespace easpace.Desktop.Features.Mood.Services;

public class MoodEntryService : IMoodEntryService
{
    private readonly List<MoodEntry> _moodEntries = [];

    public MoodEntry CreateMoodEntry(UpsertMoodEntryRequest entryRequest)
    {
        var moodEntry = new MoodEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = entryRequest.Timestamp,
            Description = entryRequest.Description,
            Labels = entryRequest.Labels,
            Value = entryRequest.Value
        };

        _moodEntries.Add(moodEntry);

        return moodEntry;
    }

    public IReadOnlyList<MoodEntry> GetMoodEntries() => _moodEntries;

    public MoodEntry? UpdateMoodEntry(Guid entryId, UpsertMoodEntryRequest entryRequest)
    {
        var existingEntry = _moodEntries.FirstOrDefault(e => e.Id == entryId);

        if (existingEntry == null) return null;

        existingEntry.Timestamp = entryRequest.Timestamp;
        existingEntry.Description = entryRequest.Description;
        existingEntry.Labels = entryRequest.Labels;
        existingEntry.Value = entryRequest.Value;

        return existingEntry;
    }

    public bool DeleteMoodEntry(Guid entryId)
    {
        var entry = _moodEntries.FirstOrDefault(entry => entry.Id == entryId);
        return entry is not null && _moodEntries.Remove(entry);
    }
}