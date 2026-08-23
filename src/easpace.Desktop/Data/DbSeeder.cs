// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Data;

public class DbSeeder
{
    private readonly AppDbContext _dbContext;

    public DbSeeder(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync()
    {
        // add default breathing techniques
        if (!await _dbContext.BreathingTechniques.AnyAsync())
        {
            var defaultTechniques = new[]
            {
                new BreathingTechnique
                {
                    Name = "BreathingTechnique.BoxBreathing.Name",
                    Description = "BreathingTechnique.BoxBreathing.Description",
                    Phases =
                    [
                        new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 4, Order = 1},
                        new BreathingPhase { Type = BreathingPhaseType.HoldIn, DurationSeconds = 4, Order = 2 },
                        new BreathingPhase { Type = BreathingPhaseType.Exhale, DurationSeconds = 4, Order = 3 },
                        new BreathingPhase { Type = BreathingPhaseType.HoldOut, DurationSeconds = 4, Order = 4 }
                    ],
                    IsLocalized = true,
                    Cycles = 4
                },
                new BreathingTechnique
                {
                    Name = "BreathingTechnique.FourSevenEightBreathing.Name",
                    Description = "BreathingTechnique.FourSevenEightBreathing.Description",
                    Phases =
                    [
                        new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 4, Order = 1},
                        new BreathingPhase { Type = BreathingPhaseType.HoldIn, DurationSeconds = 7, Order = 2 },
                        new BreathingPhase { Type = BreathingPhaseType.Exhale, DurationSeconds = 8, Order = 3 }
                    ],
                    IsLocalized = true,
                    Cycles = 4
                }
            };
            
            await _dbContext.BreathingTechniques.AddRangeAsync(defaultTechniques);
            await _dbContext.SaveChangesAsync();
        }
    }
}