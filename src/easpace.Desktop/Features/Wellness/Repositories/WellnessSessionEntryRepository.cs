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

namespace easpace.Desktop.Features.Wellness.Repositories;

internal class WellnessSessionEntryRepository(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<WellnessSessionEntryRepository> logger) : IWellnessSessionEntryRepository
{
    public async Task<WellnessSessionEntry?> CreateWellnessSessionEntryAsync(
        CreateWellnessSessionEntryRequest createSessionEntryRequest)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            logger.LogInformation("Creating new wellness session with {Name} exercise",
                createSessionEntryRequest.ExerciseName);

            var exercise = await dbContext.WellnessExercises
                .FindAsync(createSessionEntryRequest.ExerciseId);

            if (exercise is null)
            {
                logger.LogWarning("Attempted to create session with non-existent exercise with ID {Id}",
                    createSessionEntryRequest.ExerciseId);
                return null;
            }

            var session = new WellnessSessionEntry
            {
                StartDate = createSessionEntryRequest.StartDate,
                Duration = createSessionEntryRequest.Duration,
                CycleCount = createSessionEntryRequest.CycleCount,
                ExerciseId = exercise.Id,
                ExerciseName = createSessionEntryRequest.ExerciseName
            };

            dbContext.WellnessSessions.Add(session);
            await dbContext.SaveChangesAsync();

            return session;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create wellness session");
            throw;
        }
    }

    public async Task<IReadOnlyList<WellnessSessionEntry>> GetWellnessSessionEntriesAsync()
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            logger.LogInformation("Fetching all wellness sessions from database");

            var sessionEntries = await dbContext.WellnessSessions
                .Include(e => e.Exercise)
                .AsNoTracking()
                .ToListAsync();

            return sessionEntries.OrderByDescending(e => e.StartDate).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch wellness sessions from database");
            throw;
        }
    }

    public async Task<bool> DeleteWellnessSessionEntryAsync(Guid sessionId)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var entry = await dbContext.WellnessSessions.FindAsync(sessionId);

            if (entry is null)
            {
                logger.LogWarning("Attempted to delete non-existent wellness session with ID {Id}", sessionId);
                return false;
            }

            dbContext.WellnessSessions.Remove(entry);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Wellness session with ID {Id} successfully deleted", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete wellness session with ID {Id}", sessionId);
            throw;
        }
    }
}