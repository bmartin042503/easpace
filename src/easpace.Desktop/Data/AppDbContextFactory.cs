// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.IO;
using easpace.Desktop.Constants;
using easpace.Desktop.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace easpace.Desktop.Data;

internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var dirPath = AppStoragePaths.LocalPath;
        
        Directory.CreateDirectory(dirPath);

        var dbPath = Path.Combine(dirPath, "easpace.db");

        var password = SecureKeyManager.GetOrGenerateDbPassword();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString = $"Data Source={dbPath};Password={password};";
        optionsBuilder.UseSqlite(connectionString, o => o.MigrationsAssembly("easpace.Desktop"));

        return new AppDbContext(optionsBuilder.Options);
    }
}