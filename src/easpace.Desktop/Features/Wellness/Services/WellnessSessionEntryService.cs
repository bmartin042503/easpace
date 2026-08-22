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

namespace easpace.Desktop.Features.Wellness.Services;

public class WellnessSessionEntryService : IWellnessSessionEntryService
{
    private readonly AppDbContext _dbContext;

    public WellnessSessionEntryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WellnessSessionEntry> CreateWellnessSessionEntryAsync(CreateWellnessSessionEntryRequest createEntryRequest)
    {
        var wellnessSession = new WellnessSessionEntry
        {
            Id = Guid.NewGuid(),
            StartDate = createEntryRequest.StartDate,
            Type = createEntryRequest.SessionType,
            TargetDuration = createEntryRequest.TargetDuration,
            ActualDuration = createEntryRequest.ActualDuration,
            BreathingTechnique = createEntryRequest.BreathingTechnique
        };

        _dbContext.WellnessSessionEntries.Add(wellnessSession);
        await _dbContext.SaveChangesAsync();
        
        return wellnessSession;
    }

    public async Task<IReadOnlyList<WellnessSessionEntry>> GetWellnessSessionEntriesAsync()
    {
        // don't use OrderByDescending on dbContext with the CreatedAt column
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
        var sessionEntries = await _dbContext.WellnessSessionEntries
            .AsNoTracking()
            .ToListAsync();
        
        return sessionEntries.OrderByDescending(e => e.StartDate).ToList();
    }

    public async Task<bool> DeleteWellnessSessionEntryAsync(Guid entryId)
    {
        var entry = await _dbContext.WellnessSessionEntries.FindAsync(entryId);
        if (entry is null) return false;

        _dbContext.WellnessSessionEntries.Remove(entry);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
}