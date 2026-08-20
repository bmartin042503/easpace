// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services;

// Temporary in-memory implementation.
public class DataEntryService : IDataEntryService
{
    private readonly List<DataEntry> _dataEntries = [];
    
    public DataEntry? CreateDataEntry(Guid activityId, CreateDataEntryRequest createRequest)
    {
        if (activityId == Guid.Empty) return null;
        
        switch (createRequest.Type)
        {
            case DataEntryType.Numeric:

                if (createRequest.Value is null) return null;

                var numericDataEntry = new NumericDataEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = createRequest.Timestamp ?? DateTimeOffset.Now,
                    ActivityId = activityId,
                    Value = createRequest.Value.Value
                };
                
                _dataEntries.Add(numericDataEntry);
                return numericDataEntry;
            
            case DataEntryType.Routine:
                
                if (createRequest.State is null) return null;

                var routineDataEntry = new RoutineDataEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = createRequest.Timestamp ?? DateTimeOffset.Now,
                    ActivityId = activityId,
                    State = createRequest.State.Value
                };
                
                _dataEntries.Add(routineDataEntry);
                return routineDataEntry;
            
            default:
                throw new NotSupportedException($"Undefined data entry type: {createRequest.Type}");
        }
    }

    public IReadOnlyList<DataEntry> GetDataEntries(Guid activityId)
    {
        return _dataEntries.Where(e => e.ActivityId == activityId)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    public DataEntry? UpdateDataEntry(Guid entryId, UpdateDataEntryRequest updateRequest)
    {
        var entry = _dataEntries.FirstOrDefault(e => e.Id == entryId);

        switch (entry)
        {
            case null:
                return null;
            
            case NumericDataEntry numericDataEntry:

                if (updateRequest.Timestamp is not null)
                {
                    numericDataEntry.Timestamp = updateRequest.Timestamp.Value;
                }

                if (updateRequest.Value is not null)
                {
                    numericDataEntry.Value = updateRequest.Value.Value;
                }
                
                return numericDataEntry;
            
            case RoutineDataEntry routineDataEntry:

                if (updateRequest.Timestamp is not null)
                {
                    routineDataEntry.Timestamp = updateRequest.Timestamp.Value;
                }

                if (updateRequest.State is not null)
                {
                    routineDataEntry.State = updateRequest.State.Value;
                }
                
                return routineDataEntry;
            
            default:
                throw new NotSupportedException($"Undefined data entry type: {entry.GetType().Name}");
        }
    }

    public bool DeleteDataEntry(Guid entryId)
    {
        var entry = _dataEntries.FirstOrDefault(e => e.Id == entryId);
        return entry is not null && _dataEntries.Remove(entry);
    }
}