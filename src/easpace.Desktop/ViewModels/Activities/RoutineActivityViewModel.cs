// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public partial class RoutineActivityViewModel : ActivityViewModel
{
    public RoutineActivity Activity => (RoutineActivity)BaseActivity;

    [ObservableProperty] private ObservableCollection<RoutineMonth> _routineMonths = [];

    public RoutineActivityViewModel(RoutineActivity activity)
    {
        BaseActivity = activity;

        RoutineMonths = new ObservableCollection<RoutineMonth>(GetRoutineMonths());

        Activity.Entries.CollectionChanged += OnEntriesCollectionChanged;
    }

    // empty constructor for AXAML preview
    public RoutineActivityViewModel()
    {
    }

    private List<RoutineMonth> GetRoutineMonths()
    {
        var monthsList = new List<RoutineMonth>();
        var startDate = Activity.CreatedAt.Date;
        var endDate = DateTime.Today;

        if (Activity.Entries.Any())
        {
            var firstEntryDate = Activity.Entries.Min(e => e.Timestamp.Date);
            if (firstEntryDate < startDate) startDate = firstEntryDate;
            
            var lastEntryDate = Activity.Entries.Max(e => e.Timestamp.Date);
            if (lastEntryDate > endDate) endDate = lastEntryDate;
        }

        var currentMonthIter = new DateTime(startDate.Year, startDate.Month, 1);
        var endMonthIter = new DateTime(endDate.Year, endDate.Month, 1);

        while (currentMonthIter <= endMonthIter)
        {
            monthsList.Add(CreateMonth(currentMonthIter.Year, currentMonthIter.Month));
            currentMonthIter = currentMonthIter.AddMonths(1);
        }

        return monthsList;
    }
    
    private RoutineMonth CreateMonth(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthlyEntries = new List<RoutineDataEntry>();

        // populate every day of the month
        for (var day = 1; day <= daysInMonth; day++)
        {
            var targetDate = new DateTime(year, month, day);

            // look for an existing entry for this specific day
            var existingEntry = Activity.Entries.FirstOrDefault(e => e.Timestamp.Date == targetDate);

            if (existingEntry != null)
            {
                monthlyEntries.Add(existingEntry);
            }
            else
            {
                // create a dummy entry for the UI with 'None' state
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

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var affectedMonths = new HashSet<DateTime>();

        // collect the year/month combinations of all added items
        if (e.NewItems != null)
        {
            foreach (RoutineDataEntry item in e.NewItems)
            {
                affectedMonths.Add(new DateTime(item.Timestamp.Year, item.Timestamp.Month, 1));
            }
        }

        // collect the year/month combinations of all removed items
        if (e.OldItems != null)
        {
            foreach (RoutineDataEntry item in e.OldItems)
            {
                affectedMonths.Add(new DateTime(item.Timestamp.Year, item.Timestamp.Month, 1));
            }
        }

        foreach (var monthDate in affectedMonths)
        {
            // find if the month currently exists in the UI
            var existingMonth =
                RoutineMonths.FirstOrDefault(m => m.Year == monthDate.Year && m.Month == monthDate.Month);

            if (existingMonth != null)
            {
                // rebuild only this specific month
                var index = RoutineMonths.IndexOf(existingMonth);
                var updatedMonth = CreateMonth(monthDate.Year, monthDate.Month);

                // replace the item in the collection. this triggers the UI to re-render just this item
                RoutineMonths[index] = updatedMonth;
            }
            else
            {
                // if the changed item falls outside the currently tracked months (e.g., completely new bounds), 
                // it is safer to rebuild the entire calendar to maintain correct order
                GetRoutineMonths();
                break;
            }
        }
    }
}