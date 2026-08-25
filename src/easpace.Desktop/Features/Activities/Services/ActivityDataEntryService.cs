// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Activities.Services;

internal class ActivityDataEntryService : IActivityDataEntryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ActivityDataEntryService> _logger;

    public ActivityDataEntryService(AppDbContext dbContext, ILogger<ActivityDataEntryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ActivityDataEntry?> CreateDataEntryAsync(Guid activityId, CreateDataEntryRequest createRequest)
    {
        if (activityId == Guid.Empty) return null;

        try
        {
            _logger.LogInformation("Creating data entry of type {Type} for activity ID {ActivityId}", createRequest.Type, activityId);
            
            ActivityDataEntry newEntry;

            switch (createRequest.Type)
            {
                case ActivityDataEntryType.Numeric:
                    if (createRequest.Value is null) return null;

                    newEntry = new NumericActivityDataEntry
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = createRequest.Timestamp ?? DateTimeOffset.Now,
                        ActivityId = activityId,
                        Value = createRequest.Value.Value
                    };
                    break;

                case ActivityDataEntryType.Routine:
                    if (createRequest.State is null) return null;

                    newEntry = new RoutineActivityDataEntry
                    {
                        Id = Guid.NewGuid(),
                        Timestamp = createRequest.Timestamp ?? DateTimeOffset.Now,
                        ActivityId = activityId,
                        State = createRequest.State.Value
                    };
                    break;

                default:
                    throw new NotSupportedException($"Undefined data entry type: {createRequest.Type}");
            }

            _dbContext.ActivityDataEntries.Add(newEntry);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Data entry {EntryId} successfully created for activity {ActivityId}", newEntry.Id, activityId);
            return newEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create data entry for activity ID {ActivityId}", activityId);
            throw;
        }
    }

    public async Task<IReadOnlyList<ActivityDataEntry>> GetDataEntriesAsync(Guid activityId)
    {
        try
        {
            _logger.LogInformation("Fetching all data entries from database for activity ID {ActivityId}", activityId);
            
            // don't use OrderByDescending on dbContext with the CreatedAt column
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses

            var dataEntries = await _dbContext.ActivityDataEntries
                .Where(e => e.ActivityId == activityId)
                .AsNoTracking()
                .ToListAsync();

            return dataEntries.OrderBy(e => e.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data entries from database for activity ID {ActivityId}", activityId);
            throw;
        }
    }

    public async Task<ActivityDataEntry?> UpdateDataEntryAsync(Guid entryId, UpdateDataEntryRequest updateRequest)
    {
        try
        {
            var entry = await _dbContext.ActivityDataEntries.FindAsync(entryId);
            
            if (entry == null)
            {
                _logger.LogWarning("Attempted to update non-existent data entry with ID {Id}", entryId);
                return null;
            }

            switch (entry)
            {
                case NumericActivityDataEntry numericDataEntry:

                    if (updateRequest.Timestamp is not null)
                    {
                        numericDataEntry.Timestamp = updateRequest.Timestamp.Value;
                    }

                    if (updateRequest.Value is not null)
                    {
                        numericDataEntry.Value = updateRequest.Value.Value;
                    }

                    break;

                case RoutineActivityDataEntry routineDataEntry:

                    if (updateRequest.Timestamp is not null)
                    {
                        routineDataEntry.Timestamp = updateRequest.Timestamp.Value;
                    }

                    if (updateRequest.State is not null)
                    {
                        routineDataEntry.State = updateRequest.State.Value;
                    }

                    break;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Data entry with ID {Id} successfully updated", entryId);
            return entry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update data entry with ID {Id}", entryId);
            throw;
        }
    }

    public async Task<bool> DeleteDataEntryAsync(Guid entryId)
    {
        try
        {
            var dataEntry = await _dbContext.ActivityDataEntries.FindAsync(entryId);
            
            if (dataEntry is null)
            {
                _logger.LogWarning("Attempted to delete non-existent data entry with ID {Id}", entryId);
                return false;
            }

            _dbContext.ActivityDataEntries.Remove(dataEntry);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Data entry with ID {Id} successfully deleted", entryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete data entry with ID {Id}", entryId);
            throw;
        }
    }
}