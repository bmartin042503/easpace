// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services;

public interface IActivityDataEntryService
{
    Task<ActivityDataEntry?> CreateDataEntryAsync(Guid activityId, CreateDataEntryRequest createRequest);
    Task<IReadOnlyList<ActivityDataEntry>> GetDataEntriesAsync(Guid activityId);
    Task<ActivityDataEntry?> UpdateDataEntryAsync(Guid entryId, UpdateDataEntryRequest updateRequest);
    Task<bool> DeleteDataEntryAsync(Guid entryId);
}