// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.IO;
using easpace.Desktop.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace easpace.Desktop.Data;

internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "easpace");
        
        Directory.CreateDirectory(folderPath);
        
        var dbPath = Path.Combine(folderPath, "easpace.db");
        
        var password = SecureKeyManager.GetOrGenerateDbPassword();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        var connectionString = $"Data Source={dbPath};Password={password};";
        optionsBuilder.UseSqlite(connectionString, o => o.MigrationsAssembly("easpace.Desktop"));

        return new AppDbContext(optionsBuilder.Options);
    }
}