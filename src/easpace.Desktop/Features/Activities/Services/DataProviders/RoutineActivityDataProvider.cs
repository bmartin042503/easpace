// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;

namespace easpace.Desktop.Features.Activities.Services.DataProviders;

public class RoutineActivityDataProvider : IRoutineActivityDataProvider
{
    public List<RoutineMonth> GetRoutineMonths(RoutineActivity routineActivity)
    {
        var startDate = routineActivity.CreatedAt.Date;
        var endDate = DateTime.Today;

        if (routineActivity.Entries.Any())
        {
            var firstEntryDate = routineActivity.Entries.Min(e => e.Timestamp.Date);
            if (firstEntryDate < startDate) startDate = firstEntryDate;
            
            var lastEntryDate = routineActivity.Entries.Max(e => e.Timestamp.Date);
            if (lastEntryDate > endDate) endDate = lastEntryDate;
        }
        
        var monthsList = new List<RoutineMonth>();

        var currentMonthIter = new DateTime(startDate.Year, startDate.Month, 1);
        var endMonthIter = new DateTime(endDate.Year, endDate.Month, 1);
        
        var routineEntries = routineActivity.Entries.OfType<RoutineDataEntry>().ToList();

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