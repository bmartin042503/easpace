// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Wellness.Contracts;
using easpace.Desktop.Features.Wellness.Entities;

namespace easpace.Desktop.Features.Wellness.Services;

public interface IWellnessSessionEntryService
{
    WellnessSessionEntry CreateWellnessSessionEntry(CreateWellnessSessionEntryRequest createEntryRequest);
    IReadOnlyList<WellnessSessionEntry> GetWellnessSessionEntries();
    bool DeleteWellnessSessionEntry(Guid entryId);
}