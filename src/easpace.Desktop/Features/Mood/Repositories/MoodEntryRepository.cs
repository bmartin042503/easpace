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
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Mood.Repositories;

internal class MoodEntryRepository : IMoodEntryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<MoodEntryRepository> _logger;

    public MoodEntryRepository(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<MoodEntryRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<MoodEntry> CreateMoodEntryAsync(UpsertMoodEntryRequest upsertRequest)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            _logger.LogInformation("Creating new mood entry with value {Value}", upsertRequest.Value);
            
            var moodEntry = new MoodEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = upsertRequest.Timestamp,
                Description = upsertRequest.Description,
                Labels = upsertRequest.Labels,
                Value = upsertRequest.Value
            };

            dbContext.MoodEntries.Add(moodEntry);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Mood entry successfully created with ID {Id}", moodEntry.Id);
            return moodEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create mood entry");
            throw;
        }
    }

    public async Task<IReadOnlyList<MoodEntry>> GetMoodEntriesAsync()
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            _logger.LogInformation("Fetching all mood entries from database");
            
            // don't use OrderByDescending on dbContext with the CreatedAt column
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
            var entries = await dbContext.MoodEntries
                .AsNoTracking()
                .ToListAsync();

            return entries.OrderByDescending(e => e.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch mood entries from database");
            throw;
        }
    }

    public async Task<MoodEntry?> UpdateMoodEntryAsync(Guid entryId, UpsertMoodEntryRequest upsertRequest)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var existingEntry = await dbContext.MoodEntries.FindAsync(entryId);

            if (existingEntry == null)
            {
                _logger.LogWarning("Attempted to update non-existent mood entry with ID {Id}", entryId);
                return null;
            }

            existingEntry.Timestamp = upsertRequest.Timestamp;
            existingEntry.Description = upsertRequest.Description;
            existingEntry.Labels = upsertRequest.Labels;
            existingEntry.Value = upsertRequest.Value;
        
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Mood entry with ID {Id} successfully updated", entryId);
            return existingEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update mood entry with ID {Id}", entryId);
            throw;
        }
    }

    public async Task<bool> DeleteMoodEntryAsync(Guid entryId)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var entry = await dbContext.MoodEntries.FindAsync(entryId);
            
            if (entry is null)
            {
                _logger.LogWarning("Attempted to delete non-existent mood entry with ID {Id}", entryId);
                return false;
            }

            dbContext.MoodEntries.Remove(entry);
        
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Mood entry with ID {Id} successfully deleted", entryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete mood entry with ID {Id}", entryId);
            throw;
        }
    }
}