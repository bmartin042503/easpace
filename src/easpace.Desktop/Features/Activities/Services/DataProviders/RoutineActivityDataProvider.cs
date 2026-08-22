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

        while (currentMonthIter <= endMonthIter)
        {
            monthsList.Add(BuildRoutineMonth(currentMonthIter.Year, currentMonthIter.Month, routineActivity));
            currentMonthIter = currentMonthIter.AddMonths(1);
        }

        return monthsList;
    }

    public RoutineMonth BuildRoutineMonth(int year, int month, RoutineActivity routineActivity)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthlyEntries = new List<RoutineActivityDataEntry>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var targetDate = new DateTime(year, month, day);
            var existingEntry = routineActivity.Entries.FirstOrDefault(e => e.Timestamp.Date == targetDate);

            if (existingEntry is RoutineActivityDataEntry existingRoutineDataEntry)
            {
                monthlyEntries.Add(existingRoutineDataEntry);
            }
            else
            {
                monthlyEntries.Add(new RoutineActivityDataEntry
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

    public List<DateTime> GetAffectedMonths(List<RoutineMonth> builtMonths, RoutineActivity routineActivity)
    {
        if (routineActivity.Entries.Count <= 0 || builtMonths.Count <= 0) return [];

        var startDate = routineActivity.Entries.MinBy(e => e.Timestamp.Date)?.Timestamp.Date;
        var endDate = routineActivity.Entries.MaxBy(e => e.Timestamp.Date)?.Timestamp.Date;

        var currentMonthIter = new DateTime(startDate!.Value.Year, startDate.Value.Month, 1);
        var endMonthIter = new DateTime(endDate!.Value.Year, endDate.Value.Month, 1);

        List<DateTime> affectedMonths = [];

        while (currentMonthIter <= endMonthIter)
        {
            if (!routineActivity.Entries.Any(e =>
                    e.Timestamp.Date.Year == currentMonthIter.Year && e.Timestamp.Date.Month == currentMonthIter.Month))
            {
                currentMonthIter = currentMonthIter.AddMonths(1);
                continue;
            }

            var builtMonthEntries = builtMonths.FirstOrDefault(m =>
                    m.Year == currentMonthIter.Year && m.Month == currentMonthIter.Month)?.Entries
                .Where(e => e.State != RoutineState.None);

            HashSet<RoutineActivityDataEntry> builtMonthEntriesSet = new();
            HashSet<RoutineActivityDataEntry> activityEntriesSet;

            if (builtMonthEntries != null)
            {
                var routineDataEntries = builtMonthEntries.ToList();
                builtMonthEntriesSet = new(routineDataEntries);
            }

            var iter = currentMonthIter;

            var activityEntries = routineActivity
                .Entries.Where(m =>
                    m.Timestamp.Date.Year == iter.Year && m.Timestamp.Date.Month == iter.Month)
                .OfType<RoutineActivityDataEntry>();

            activityEntriesSet = new(activityEntries);

            var affectedMonthEntries = activityEntriesSet.Except(builtMonthEntriesSet);
            if (affectedMonthEntries.Any())
            {
                affectedMonths.Add(new DateTime(iter.Year, iter.Month, 1));
            }

            currentMonthIter = currentMonthIter.AddMonths(1);
        }

        return affectedMonths;
    }
}