// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.IO;
using easpace.Desktop.Data;
using easpace.Desktop.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Services.Data;

internal class DataWipeService(AppDbContext dbContext, ILogger<DataWipeService> logger) : IDataWipeService
{
    public void DeleteEncryptionKey()
    {
        SecureKeyManager.DeleteDbPassword();
    }

    public void DeleteDatabaseFile()
    {
        logger.LogInformation("Closing database connection");

        dbContext.Database.CloseConnection();
        
        // clear all pools, otherwise we got an SQLite exception (database file is used by another process)
        SqliteConnection.ClearAllPools();

        try
        {
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "easpace");
            var dbPath = Path.Combine(folderPath, "easpace.db");

            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
                logger.LogInformation("Database deleted");
            }

            if (File.Exists(dbPath + "-shm")) File.Delete(dbPath + "-shm");
            if (File.Exists(dbPath + "-wal")) File.Delete(dbPath + "-wal");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured while trying to delete the database file");
            throw;
        }
    }

    public void DeletePreferencesFile()
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "easpace");
            var preferencesPath = Path.Combine(folder, "preferences.json");

            if (!File.Exists(preferencesPath)) return;
            File.Delete(preferencesPath);
            
            logger.LogInformation("Preferences file deleted");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured while trying to delete the database file");
            throw;
        }
    }
}