// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

public interface IWellnessSessionEntryService
{
    Task<WellnessSessionEntry> CreateWellnessSessionEntryAsync(CreateWellnessSessionEntryRequest createEntryRequest);
    Task<IReadOnlyList<WellnessSessionEntry>> GetWellnessSessionEntriesAsync();
    Task<bool> DeleteWellnessSessionEntryAsync(Guid entryId);
}