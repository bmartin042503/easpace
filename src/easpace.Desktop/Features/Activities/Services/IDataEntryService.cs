// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services;

public interface IDataEntryService
{
    DataEntry? CreateDataEntry(Guid activityId, CreateDataEntryRequest createRequest);
    IReadOnlyList<DataEntry> GetDataEntries(Guid activityId);
    DataEntry? UpdateDataEntry(Guid entryId, UpdateDataEntryRequest updateRequest);
    bool DeleteDataEntry(Guid entryId);
}