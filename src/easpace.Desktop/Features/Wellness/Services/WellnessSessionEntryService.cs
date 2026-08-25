// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Wellness.Services;

internal class WellnessSessionEntryService : IWellnessSessionEntryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WellnessSessionEntryService> _logger;

    public WellnessSessionEntryService(AppDbContext dbContext, ILogger<WellnessSessionEntryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<WellnessSessionEntry> CreateWellnessSessionEntryAsync(
        CreateWellnessSessionEntryRequest createEntryRequest)
    {
        try
        {
            _logger.LogInformation("Creating new wellness session entry of type {Type}", createEntryRequest.SessionType);
            
            var wellnessSession = new WellnessSessionEntry
            {
                Id = Guid.NewGuid(),
                StartDate = createEntryRequest.StartDate,
                Type = createEntryRequest.SessionType,
                TargetDuration = createEntryRequest.TargetDuration,
                ActualDuration = createEntryRequest.ActualDuration,
                BreathingTechniqueId = createEntryRequest.BreathingTechnique?.Id
            };

            _dbContext.WellnessSessionEntries.Add(wellnessSession);
            await _dbContext.SaveChangesAsync();

            return wellnessSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create wellness session entry");
            throw;
        }
    }

    public async Task<IReadOnlyList<WellnessSessionEntry>> GetWellnessSessionEntriesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all wellness session entries from database");
            
            // don't use OrderByDescending on dbContext with the CreatedAt column
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses

            var sessionEntries = await _dbContext.WellnessSessionEntries
                .Include(e => e.BreathingTechnique)
                .ThenInclude(t => t.Phases.OrderBy(p => p.Order))
                .AsNoTracking()
                .ToListAsync();

            return sessionEntries.OrderByDescending(e => e.StartDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch wellness session entries from database");
            throw;
        }
    }

    public async Task<bool> DeleteWellnessSessionEntryAsync(Guid entryId)
    {
        try
        {
            var entry = await _dbContext.WellnessSessionEntries.FindAsync(entryId);
            
            if (entry is null)
            {
                _logger.LogWarning("Attempted to delete non-existent wellness session entry with ID {Id}", entryId);
                return false;
            }

            _dbContext.WellnessSessionEntries.Remove(entry);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Wellness session entry with ID {Id} successfully deleted", entryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete wellness session entry with ID {Id}", entryId);
            throw;
        }
    }
}