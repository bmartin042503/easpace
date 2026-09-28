// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Features.Wellness.Entities;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Data;

internal class DbSeeder
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DbSeeder(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SeedAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // add default breathing techniques if there's none
        if (!await dbContext.BreathingTechniques.AnyAsync())
        {
            var defaultTechniques = new[]
            {
                new BreathingTechnique
                {
                    Name = "BreathingTechnique.BoxBreathing.Name",
                    Description = "BreathingTechnique.BoxBreathing.Description",
                    Phases =
                    [
                        new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 4, Order = 1 },
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
                        new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 4, Order = 1 },
                        new BreathingPhase { Type = BreathingPhaseType.HoldIn, DurationSeconds = 7, Order = 2 },
                        new BreathingPhase { Type = BreathingPhaseType.Exhale, DurationSeconds = 8, Order = 3 }
                    ],
                    IsLocalized = true,
                    Cycles = 4
                },
                new BreathingTechnique
                {
                    Name = "BreathingTechnique.FourSixBreathing.Name",
                    Description = "BreathingTechnique.FourSixBreathing.Description",
                    Phases =
                    [
                        new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 4, Order = 1 },
                        new BreathingPhase { Type = BreathingPhaseType.Exhale, DurationSeconds = 6, Order = 2 }
                    ],
                    IsLocalized = true,
                    Cycles = 5
                },
                new BreathingTechnique
                {
                    Name = "BreathingTechnique.TriangleBreathing.Name",
                    Description = "BreathingTechnique.TriangleBreathing.Description",
                    Phases =
                    [
                        new BreathingPhase { Type = BreathingPhaseType.Inhale, DurationSeconds = 3, Order = 1 },
                        new BreathingPhase { Type = BreathingPhaseType.HoldIn, DurationSeconds = 3, Order = 2 },
                        new BreathingPhase { Type = BreathingPhaseType.Exhale, DurationSeconds = 3, Order = 3 }
                    ],
                    IsLocalized = true,
                    Cycles = 6
                }
            };
            
            await dbContext.BreathingTechniques.AddRangeAsync(defaultTechniques);
            await dbContext.SaveChangesAsync();
        }
    }
}