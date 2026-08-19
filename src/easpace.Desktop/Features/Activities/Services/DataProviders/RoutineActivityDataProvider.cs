// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

public class RoutineActivityDataProvider : IRoutineActivityDataProvider
{
    public List<RoutineMonth> GetRoutineMonths(List<RoutineDataEntry> routineEntries)
    {
        var firstEntryDate = routineEntries.Min(e => e.Timestamp.Date);
        var lastEntryDate = routineEntries.Max(e => e.Timestamp.Date);
        
        var monthsList = new List<RoutineMonth>();

        var currentMonthIter = new DateTime(firstEntryDate.Year, firstEntryDate.Month, 1);
        var endMonthIter = new DateTime(lastEntryDate.Year, lastEntryDate.Month, 1);

        while (currentMonthIter <= endMonthIter)
        {
            monthsList.Add(CreateRoutineMonth(currentMonthIter.Year, currentMonthIter.Month, routineEntries));
            currentMonthIter = currentMonthIter.AddMonths(1);
        }

        return monthsList;
    }

    private RoutineMonth CreateRoutineMonth(int year, int month, List<RoutineDataEntry> routineEntries)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthlyEntries = new List<RoutineDataEntry>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var targetDate = new DateTime(year, month, day);
            var existingEntry = routineEntries.FirstOrDefault(e => e.Timestamp.Date == targetDate);

            if (existingEntry != null)
            {
                monthlyEntries.Add(existingEntry);
            }
            else
            {
                monthlyEntries.Add(new RoutineDataEntry
                {
                    Timestamp = targetDate,
                    State = RoutineState.None
                });
            }
        }

        return new RoutineMonth
        {
            Year = year,
            Month = month,
            Entries = monthlyEntries
        };
    }
}