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

namespace easpace.Desktop.Features.Activities.Services;

public class ActivityDataEntryService : IActivityDataEntryService
{
    private readonly AppDbContext _dbContext;

    public ActivityDataEntryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<ActivityDataEntry?> CreateDataEntryAsync(Guid activityId, CreateDataEntryRequest createRequest)
    {
        if (activityId == Guid.Empty) return null;
    
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
    
        return newEntry;
    }

    public async Task<IReadOnlyList<ActivityDataEntry>> GetDataEntriesAsync(Guid activityId)
    {
        // don't use OrderByDescending on dbContext with the CreatedAt column
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
        var dataEntries = await _dbContext.ActivityDataEntries
            .Where(e => e.ActivityId == activityId)
            .AsNoTracking()
            .ToListAsync();
        
        return dataEntries.OrderBy(e => e.Timestamp).ToList();
    }

    public async Task<ActivityDataEntry?> UpdateDataEntryAsync(Guid entryId, UpdateDataEntryRequest updateRequest)
    {
        var entry = await _dbContext.ActivityDataEntries.FindAsync(entryId);

        switch (entry)
        {
            case null: return null;
            
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
        return entry;
    }

    public async Task<bool> DeleteDataEntryAsync(Guid entryId)
    {
        var dataEntry = await _dbContext.ActivityDataEntries.FindAsync(entryId);
        if (dataEntry is null) return false;

        _dbContext.ActivityDataEntries.Remove(dataEntry);
        
        await _dbContext.SaveChangesAsync();
        return true;
    }
}