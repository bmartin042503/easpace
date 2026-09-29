// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Data;

internal class DbSeeder
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IPreferencesService _preferencesService;

    public DbSeeder(IDbContextFactory<AppDbContext> dbContextFactory, IPreferencesService preferencesService)
    {
        _dbContextFactory = dbContextFactory;
        _preferencesService = preferencesService;
    }

    /// <summary>
    /// Seeds the default wellness exercises once. Each session type is seeded only if it has no exercises yet,
    /// so upgraded databases keep their migrated exercises and deleted defaults are never re-added.
    /// </summary>
    public async Task SeedAsync()
    {
        if (_preferencesService.ReadPreference<bool>(PreferenceKey.WellnessDefaultsSeeded)) return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var existingTypes = (await dbContext.WellnessExercises.AsNoTracking().ToListAsync())
            .Select(e => e.SessionType)
            .ToHashSet();

        var missingDefaults = DefaultWellnessExercises.Create()
            .Where(e => !existingTypes.Contains(e.SessionType))
            .ToList();

        if (missingDefaults.Count > 0)
        {
            dbContext.WellnessExercises.AddRange(missingDefaults);
            await dbContext.SaveChangesAsync();
        }

        // only mark as seeded once the defaults are saved, so a failed attempt is retried on the next launch
        _preferencesService.SavePreference(PreferenceKey.WellnessDefaultsSeeded, true);
    }
}