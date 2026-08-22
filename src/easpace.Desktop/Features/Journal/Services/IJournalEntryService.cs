// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using easpace.Desktop.Features.Journal.Entities;

namespace easpace.Desktop.Features.Journal.Services;

public interface IJournalEntryService
{
    Task<JournalEntry> CreateJournalEntryAsync(string title, string content);
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesAsync();
    Task<JournalEntry?> UpdateJournalEntryAsync(Guid entryId, string title, string content);
    Task<bool> DeleteJournalEntryAsync(Guid entryId);
}