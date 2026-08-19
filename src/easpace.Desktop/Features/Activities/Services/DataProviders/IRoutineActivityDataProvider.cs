// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

public interface IRoutineActivityDataProvider
{
    List<RoutineMonth> GetRoutineMonths(List<RoutineDataEntry> routineEntries);
}