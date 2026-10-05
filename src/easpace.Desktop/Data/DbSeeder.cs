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

        // TODO: research on what default exercises to add and seed them here once
    }
}