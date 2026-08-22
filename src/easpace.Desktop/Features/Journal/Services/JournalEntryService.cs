// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Journal.Entities;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Features.Journal.Services;

public class JournalEntryService : IJournalEntryService
{
    private readonly AppDbContext _dbContext;

    public JournalEntryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JournalEntry> CreateJournalEntryAsync(string title, string content)
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            CreatedAt = DateTimeOffset.Now,
        };

        _dbContext.JournalEntries.Add(entry);
        await _dbContext.SaveChangesAsync();
        
        return entry;
    }

    public async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesAsync()
    {
        // don't use OrderByDescending on dbContext with the CreatedAt column
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
        var entries = await _dbContext.JournalEntries
            .AsNoTracking()
            .ToListAsync();
        
        return entries.OrderByDescending(e => e.CreatedAt).ToList();
    }

    public async Task<JournalEntry?> UpdateJournalEntryAsync(Guid entryId, string title, string content)
    {
        var entry = await _dbContext.JournalEntries.FindAsync(entryId);
        
        if (entry is null) return null;

        entry.Title = title;
        entry.Content = content;
        
        await _dbContext.SaveChangesAsync();

        return entry;
    }

    public async Task<bool> DeleteJournalEntryAsync(Guid entryId)
    {
        var entry = await _dbContext.JournalEntries.FindAsync(entryId);
        if (entry is null) return false;
        
        _dbContext.JournalEntries.Remove(entry);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}