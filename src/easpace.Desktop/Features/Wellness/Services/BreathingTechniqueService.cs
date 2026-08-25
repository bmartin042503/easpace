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

internal class BreathingTechniqueService : IBreathingTechniqueService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BreathingTechniqueService> _logger;

    public BreathingTechniqueService(AppDbContext dbContext, ILogger<BreathingTechniqueService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BreathingTechnique> CreateBreathingTechniqueAsync(UpsertBreathingTechniqueRequest upsertRequest)
    {
        try
        {
            _logger.LogInformation("Creating new breathing technique '{Name}'", upsertRequest.Name);
            
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
            _logger.LogInformation("Breathing technique created successfully with ID {Id}", breathingTechnique.Id);
            return breathingTechnique;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create breathing technique");
            throw;
        }
    }

    public async Task<IReadOnlyList<BreathingTechnique>> GetBreathingTechniquesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all breathing techniques from database");
            
            // don't use OrderByDescending on dbContext with the CreatedAt column
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
            var techniques = await _dbContext.BreathingTechniques
                .Include(t => t.Phases.OrderBy(p => p.Order))
                .AsNoTracking()
                .ToListAsync();
        
            return techniques.OrderByDescending(t => t.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch breathing techniques from database");
            throw;
        }
    }

    public async Task<BreathingTechnique?> UpdateBreathingTechniqueAsync(Guid techniqueId, UpsertBreathingTechniqueRequest upsertRequest)
    {
        try
        {
            var technique = await _dbContext.BreathingTechniques
                .Include(t => t.Phases)
                .FirstOrDefaultAsync(t => t.Id == techniqueId);

            if (technique == null)
            {
                _logger.LogWarning("Attempted to update non-existent breathing technique with ID {Id}", techniqueId);
                return null;
            }

            technique.Name = upsertRequest.Name;
            technique.Description = upsertRequest.Description;
            technique.Cycles = upsertRequest.Cycles;

            _dbContext.BreathingPhases.RemoveRange(technique.Phases);
            technique.Phases = upsertRequest.Phases;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Breathing technique with ID {Id} successfully updated", techniqueId);
            return technique;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update breathing technique with ID {Id}", techniqueId);
            throw;
        }
    }

    public async Task<bool> DeleteBreathingTechniqueAsync(Guid techniqueId)
    {
        try
        {
            var technique = await _dbContext.BreathingTechniques.FindAsync(techniqueId);
            
            if (technique is null)
            {
                _logger.LogWarning("Attempted to delete non-existent breathing technique with ID {Id}", techniqueId);
                return false;
            }

            _dbContext.BreathingTechniques.Remove(technique);
        
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Breathing technique with ID {Id} successfully deleted", techniqueId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete breathing technique with ID {Id}", techniqueId);
            throw;
        }
    }
}