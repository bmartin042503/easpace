// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

internal interface IRoutineActivityDataProvider
{
    List<RoutineMonth> GetRoutineMonths(RoutineActivity routineActivity);
    RoutineMonth BuildRoutineMonth(int year, int month, RoutineActivity routineActivity);
    List<DateTime> GetAffectedMonths(List<RoutineMonth> builtMonths, RoutineActivity routineActivity);
}