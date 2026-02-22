using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Services;

namespace easpace.Converters;

public class DateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime) return null;
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalSeconds < 60)
        {
            return LocalizationService.GetString("JUST_NOW");
        }

        if (timeSpan.TotalMinutes < 60)
        {
            return string.Format(LocalizationService.GetString("MINUTES_AGO"), (int)timeSpan.TotalMinutes);
        }

        if (timeSpan.TotalHours < 24)
        {
            return string.Format(LocalizationService.GetString("HOURS_AGO"), (int)timeSpan.TotalHours);
        }

        if (timeSpan.TotalDays < 30)
        {
            return string.Format(LocalizationService.GetString("DAYS_AGO"), (int)timeSpan.TotalDays);
        }

        if (timeSpan.TotalDays < 365)
        {
            var months = (int)(timeSpan.TotalDays / 30);
            return string.Format(LocalizationService.GetString("MONTHS_AGO"), months);
        }

        var years = (int)(timeSpan.TotalDays / 365);
        return string.Format(LocalizationService.GetString("YEARS_AGO"), years);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Relative date time cannot be converted back."));
    }
}
