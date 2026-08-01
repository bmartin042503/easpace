using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Constants;
using easpace.Desktop.Services;

namespace easpace.Desktop.Converters;

public class TimeRangeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChartTimeRange timeRange) return value?.ToString();
        
        var localizationKey = timeRange.ToString().ToUpper();
        
        return LocalizationService.GetString(localizationKey);

    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Localized Time Range value cannot be converted back."));
    }
}