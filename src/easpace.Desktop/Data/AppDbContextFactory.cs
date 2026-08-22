// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace easpace.Desktop.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "easpace");
        
        Directory.CreateDirectory(folderPath);
        
        var dbPath = Path.Combine(folderPath, "easpace.db");
        
        // this will be changed later and integrated with the Credentials Manager
        var password = "TemporaryPassword12345";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite($"Data Source=file:{dbPath};Password={password}");

        return new AppDbContext(optionsBuilder.Options);
    }
}