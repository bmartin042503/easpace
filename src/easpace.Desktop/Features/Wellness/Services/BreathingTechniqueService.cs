// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Features.Wellness.Services;

public class BreathingTechniqueService : IBreathingTechniqueService
{
    private readonly AppDbContext _dbContext;

    public BreathingTechniqueService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BreathingTechnique> CreateBreathingTechniqueAsync(UpsertBreathingTechniqueRequest upsertRequest)
    {
        var breathingTechnique = new BreathingTechnique
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Name = upsertRequest.Name,
            Description = upsertRequest.Description,
            Phases = upsertRequest.Phases,
            Cycles = upsertRequest.Cycles,
        };

        _dbContext.BreathingTechniques.Add(breathingTechnique);
        await _dbContext.SaveChangesAsync();

        return breathingTechnique;
    }

    public async Task<IReadOnlyList<BreathingTechnique>> GetBreathingTechniquesAsync()
    {
        // don't use OrderByDescending on dbContext with the CreatedAt column
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
        var techniques = await _dbContext.BreathingTechniques
            .Include(t => t.Phases)
            .AsNoTracking()
            .ToListAsync();
        
        return techniques.OrderByDescending(t => t.CreatedAt).ToList();
    }

    public async Task<BreathingTechnique?> UpdateBreathingTechniqueAsync(Guid techniqueId, UpsertBreathingTechniqueRequest upsertRequest)
    {
        var technique = await _dbContext.BreathingTechniques
            .Include(t => t.Phases)
            .FirstOrDefaultAsync(t => t.Id == techniqueId);

        if (technique == null) return null;

        technique.Name = upsertRequest.Name;
        technique.Description = upsertRequest.Description;
        technique.Cycles = upsertRequest.Cycles;

        _dbContext.BreathingPhases.RemoveRange(technique.Phases);
        technique.Phases = upsertRequest.Phases;

        await _dbContext.SaveChangesAsync();

        return technique;
    }

    public async Task<bool> DeleteBreathingTechniqueAsync(Guid techniqueId)
    {
        var technique = await _dbContext.BreathingTechniques.FindAsync(techniqueId);
        if (technique is null) return false;

        _dbContext.BreathingTechniques.Remove(technique);
        
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
}