// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Journal.Entities;

namespace easpace.Desktop.Features.Journal.Services;

public interface IJournalService
{
    JournalEntry CreateJournalEntry(string title, string content);
    IReadOnlyList<JournalEntry> GetJournalEntries();
    JournalEntry? UpdateJournalEntry(Guid entryId, string title, string content);
    bool DeleteJournalEntry(Guid entryId);
}