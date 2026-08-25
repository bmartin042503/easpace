// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.Features.Activities.ViewModels.Dialogs;

internal partial class EntryDialogViewModel : DialogViewModel
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _confirmText = string.Empty;
    [ObservableProperty] private string _cancelText = string.Empty;
    [ObservableProperty] private bool _confirmed;
    [ObservableProperty] private DateTime? _selectedDate = DateTime.Now;
    [ObservableProperty] private TimeSpan? _selectedTime = DateTime.Now.TimeOfDay;
    
    public DateTime MaxAllowedDate => DateTime.Today.AddDays(1);
    
    partial void OnSelectedDateChanged(DateTime? value)
    {
        if (value.HasValue && value.Value.Date > MaxAllowedDate.Date)
        {
            SelectedDate = MaxAllowedDate;
        }
    }
    
    public DateTimeOffset GetTimestamp()
    {
        var date = SelectedDate?.Date ?? DateTime.Today;
        var time = SelectedTime ?? DateTime.Now.TimeOfDay;
        return new DateTimeOffset(date.Add(time));
    }
    
    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
        Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        Confirmed = false;
        Close();
    }
}