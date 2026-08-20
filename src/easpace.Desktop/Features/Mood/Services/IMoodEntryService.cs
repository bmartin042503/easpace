// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Mood.Contracts;
using easpace.Desktop.Features.Mood.Entities;

namespace easpace.Desktop.Features.Mood.Services;

public interface IMoodEntryService
{
    MoodEntry CreateMoodEntry(UpsertMoodEntryRequest entryRequest);
    IReadOnlyList<MoodEntry> GetMoodEntries();
    MoodEntry? UpdateMoodEntry(Guid entryId, UpsertMoodEntryRequest entryRequest);
    bool DeleteMoodEntry(Guid entryId);
}