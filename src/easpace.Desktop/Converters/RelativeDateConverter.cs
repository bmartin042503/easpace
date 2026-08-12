// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Services;

namespace easpace.Desktop.Converters;

public class RelativeDateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset date) return value?.ToString();
        
        var timeSince = DateTimeOffset.Now - date;

        if (!(timeSince.TotalDays < 1)) return date.ToString("g", culture);

        if (timeSince.TotalMinutes < 1)
        {
            return LocalizationService.GetString("Common.Time.JustNow");
        }

        if (timeSince.TotalHours < 1)
        {
            var minutes = (int)timeSince.TotalMinutes;
            var key = minutes == 1 ? "Common.Time.OneMinuteAgo" : "Common.Time.MinutesAgo";
            return string.Format(LocalizationService.GetString(key), minutes);
        }

        var hours = (int)timeSince.TotalHours;
        var hourKey = hours == 1 ? "Common.Time.OneHourAgo" : "Common.Time.HoursAgo";
        return string.Format(LocalizationService.GetString(hourKey), hours);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Relative date time cannot be converted back."));
    }
}