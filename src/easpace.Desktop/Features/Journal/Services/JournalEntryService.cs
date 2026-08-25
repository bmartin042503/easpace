// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Journal.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Journal.Services;

internal class JournalEntryService : IJournalEntryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<JournalEntryService> _logger;

    public JournalEntryService(AppDbContext dbContext, ILogger<JournalEntryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<JournalEntry> CreateJournalEntryAsync(string title, string content)
    {
        try
        {
            _logger.LogInformation("Creating new journal entry with title '{Title}'", title);
            
            var entry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                CreatedAt = DateTimeOffset.Now,
            };

            _dbContext.JournalEntries.Add(entry);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Journal entry successfully created with ID {Id}", entry.Id);
            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create journal entry");
            throw;
        }
    }

    public async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all journal entries from database");
            
            // don't use OrderByDescending on dbContext with the CreatedAt column
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
            var entries = await _dbContext.JournalEntries
                .AsNoTracking()
                .ToListAsync();
        
            return entries.OrderByDescending(e => e.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch journal entries from database");
            throw;
        }
    }

    public async Task<JournalEntry?> UpdateJournalEntryAsync(Guid entryId, string title, string content)
    {
        try
        {
            var entry = await _dbContext.JournalEntries.FindAsync(entryId);
        
            if (entry is null)
            {
                _logger.LogWarning("Attempted to update non-existent journal entry with ID {Id}", entryId);
                return null;
            }

            entry.Title = title;
            entry.Content = content;
        
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Journal entry with ID {Id} successfully updated", entryId);
            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update journal entry with ID {Id}", entryId);
            throw;
        }
    }

    public async Task<bool> DeleteJournalEntryAsync(Guid entryId)
    {
        try
        {
            var entry = await _dbContext.JournalEntries.FindAsync(entryId);
            
            if (entry is null)
            {
                _logger.LogWarning("Attempted to delete non-existent journal entry with ID {Id}", entryId);
                return false;
            }
        
            _dbContext.JournalEntries.Remove(entry);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Journal entry with ID {Id} successfully deleted", entryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete journal entry with ID {Id}", entryId);
            throw;
        }
    }
}