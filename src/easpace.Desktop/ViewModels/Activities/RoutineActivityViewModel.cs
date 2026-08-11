// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels.Activities;

public partial class RoutineActivityViewModel : ActivityViewModel
{
    private readonly IDialogService _dialogService;
    
    public RoutineActivity Activity => (RoutineActivity)BaseActivity;

    [ObservableProperty] private ObservableCollection<RoutineMonth> _routineMonths = [];

    public RoutineActivityViewModel(RoutineActivity activity, IDialogService dialogService)
    {
        BaseActivity = activity;
        
        _dialogService = dialogService;
        
        RoutineMonths = new ObservableCollection<RoutineMonth>(GetRoutineMonths());
        Activity.Entries.CollectionChanged += OnEntriesCollectionChanged;
    }

    [RelayCommand]
    private async Task AddRoutineEntry()
    {
        var routineEntryDialog = new RoutineEntryDialogViewModel
        {
            Title = LocalizationService.GetString("Activities.EntryDialog.Title"),
            CancelText = LocalizationService.GetString("Common.Button.Cancel"),
            ConfirmText = LocalizationService.GetString("Common.Button.Save"),
            SelectedDate = DateTime.Now,
            SelectedState = RoutineState.Completed
        };

        await _dialogService.ShowDialogAsync(routineEntryDialog);

        if (routineEntryDialog.Confirmed)
        {
            var timestamp = routineEntryDialog.GetTimestamp();
            var state = routineEntryDialog.SelectedState;

            var existingEntry = Activity.Entries.FirstOrDefault(e => e.Timestamp.Date == timestamp.Date);

            if (existingEntry != null)
            {
                if (state is RoutineState.None)
                {
                    Activity.Entries.Remove(existingEntry);
                    return;
                }

                var replaceEntry = new RoutineDataEntry
                {
                    Id = existingEntry.Id,
                    Timestamp = routineEntryDialog.GetTimestamp(),
                    State = state
                };

                var index = Activity.Entries.IndexOf(existingEntry);
                if (index >= 0)
                {
                    Activity.Entries[index] = replaceEntry;
                }
            }
            else
            {
                if (state is RoutineState.None) return;

                var newEntry = new RoutineDataEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = routineEntryDialog.GetTimestamp(),
                    State = state
                };
                Activity.Entries.Add(newEntry);
            }
        }
    }

    private (DateTime Start, DateTime End) GetCalendarBounds()
    {
        var startDate = Activity.CreatedAt.Date;
        var endDate = DateTime.Today;

        if (!Activity.Entries.Any()) return (startDate, endDate);

        var firstEntryDate = Activity.Entries.Min(e => e.Timestamp.Date);
        if (firstEntryDate < startDate) startDate = firstEntryDate;

        var lastEntryDate = Activity.Entries.Max(e => e.Timestamp.Date);
        if (lastEntryDate > endDate) endDate = lastEntryDate;

        return (startDate, endDate);
    }

    private List<RoutineMonth> GetRoutineMonths()
    {
        var monthsList = new List<RoutineMonth>();
        var bounds = GetCalendarBounds();

        var currentMonthIter = new DateTime(bounds.Start.Year, bounds.Start.Month, 1);
        var endMonthIter = new DateTime(bounds.End.Year, bounds.End.Month, 1);

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

        for (var day = 1; day <= daysInMonth; day++)
        {
            var targetDate = new DateTime(year, month, day);
            var existingEntry = Activity.Entries.FirstOrDefault(e => e.Timestamp.Date == targetDate);

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

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (RoutineMonths.Count > 0)
        {
            var bounds = GetCalendarBounds();
            var expectedStartMonth = new DateTime(bounds.Start.Year, bounds.Start.Month, 1);
            var expectedEndMonth = new DateTime(bounds.End.Year, bounds.End.Month, 1);

            var currentStartMonth = new DateTime(RoutineMonths.First().Year, RoutineMonths.First().Month, 1);
            var currentEndMonth = new DateTime(RoutineMonths.Last().Year, RoutineMonths.Last().Month, 1);

            if (expectedStartMonth != currentStartMonth || expectedEndMonth != currentEndMonth)
            {
                // rebuild the entire calendar if the bounds have been changed
                RoutineMonths = new ObservableCollection<RoutineMonth>(GetRoutineMonths());
                return;
            }
        }
        else
        {
            RoutineMonths = new ObservableCollection<RoutineMonth>(GetRoutineMonths());
            return;
        }

        // if the bounds didn't change, we only update months that are already in the collection
        var affectedMonths = new HashSet<DateTime>();

        if (e.NewItems != null)
        {
            foreach (RoutineDataEntry item in e.NewItems)
                affectedMonths.Add(new DateTime(item.Timestamp.Year, item.Timestamp.Month, 1));
        }

        if (e.OldItems != null)
        {
            foreach (RoutineDataEntry item in e.OldItems)
                affectedMonths.Add(new DateTime(item.Timestamp.Year, item.Timestamp.Month, 1));
        }

        foreach (var monthDate in affectedMonths)
        {
            var existingMonth =
                RoutineMonths.FirstOrDefault(m => m.Year == monthDate.Year && m.Month == monthDate.Month);

            if (existingMonth == null) continue;

            var index = RoutineMonths.IndexOf(existingMonth);
            RoutineMonths[index] = CreateMonth(monthDate.Year, monthDate.Month);
        }
    }
}