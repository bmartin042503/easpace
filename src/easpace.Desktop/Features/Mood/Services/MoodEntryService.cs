// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Mood.Contracts;
using easpace.Desktop.Features.Mood.Entities;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Features.Mood.Services;

public class MoodEntryService : IMoodEntryService
{
    private readonly AppDbContext _dbContext;

    public MoodEntryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MoodEntry> CreateMoodEntryAsync(UpsertMoodEntryRequest upsertRequest)
    {
        var moodEntry = new MoodEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = upsertRequest.Timestamp,
            Description = upsertRequest.Description,
            Labels = upsertRequest.Labels,
            Value = upsertRequest.Value
        };

        _dbContext.MoodEntries.Add(moodEntry);
        await _dbContext.SaveChangesAsync();

        return moodEntry;
    }

    public async Task<IReadOnlyList<MoodEntry>> GetMoodEntriesAsync()
    {
        // don't use OrderByDescending on dbContext with the CreatedAt column
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
        var entries = await _dbContext.MoodEntries
            .AsNoTracking()
            .ToListAsync();

        return entries.OrderByDescending(e => e.Timestamp).ToList();
    }

    public async Task<MoodEntry?> UpdateMoodEntryAsync(Guid entryId, UpsertMoodEntryRequest upsertRequest)
    {
        var existingEntry = await _dbContext.MoodEntries.FindAsync(entryId);

        if (existingEntry == null) return null;

        existingEntry.Timestamp = upsertRequest.Timestamp;
        existingEntry.Description = upsertRequest.Description;
        existingEntry.Labels = upsertRequest.Labels;
        existingEntry.Value = upsertRequest.Value;
        
        await _dbContext.SaveChangesAsync();

        return existingEntry;
    }

    public async Task<bool> DeleteMoodEntryAsync(Guid entryId)
    {
        var entry = await _dbContext.MoodEntries.FindAsync(entryId);
        if (entry is null) return false;

        _dbContext.MoodEntries.Remove(entry);
        
        await _dbContext.SaveChangesAsync();
        return true;
    }
}