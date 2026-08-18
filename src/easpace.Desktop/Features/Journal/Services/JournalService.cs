// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Journal.Entities;

namespace easpace.Desktop.Features.Journal.Services;

// Temporary in-memory implementation.
public class JournalService : IJournalService
{
    private readonly List<JournalEntry> _journalEntries = [];

    public JournalEntry CreateJournalEntry(string title, string content)
    {
        var entry = new JournalEntry
        {
            Title = title,
            Content = content,
            CreatedAt = DateTimeOffset.Now,
        };

        _journalEntries.Add(entry);
        return entry;
    }

    public IReadOnlyList<JournalEntry> GetJournalEntries() =>
        _journalEntries
            .OrderByDescending(entry => entry.CreatedAt)
            .ToList();

    public JournalEntry? UpdateJournalEntry(Guid entryId, string title, string content)
    {
        var entry = _journalEntries.FirstOrDefault(entry => entry.Id == entryId);
        if (entry is null)
        {
            return null;
        }

        entry.Title = title;
        entry.Content = content;

        return entry;
    }

    public bool DeleteJournalEntry(Guid entryId)
    {
        var entry = _journalEntries.FirstOrDefault(entry => entry.Id == entryId);
        return entry is not null && _journalEntries.Remove(entry);
    }
}