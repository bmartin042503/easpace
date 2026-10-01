// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
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
    /// Seeds the default wellness exercises once, with their texts in the current app language. Deleted defaults are
    /// never re-added, and a default is skipped if an exercise already has its name, so upgraded databases keep their
    /// migrated exercises without duplicates.
    /// </summary>
    public async Task SeedAsync()
    {
        if (_preferencesService.ReadPreference<bool>(PreferenceKey.WellnessDefaultsSeeded)) return;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // migrated legacy techniques are named in the same language, as the migration runs right before seeding
        var existingNames = (await dbContext.WellnessExercises.AsNoTracking().Select(e => e.Name).ToListAsync())
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        var missingDefaults = DefaultWellnessExercises.Create()
            .Where(e => !existingNames.Contains(e.Name))
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