// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Converters;

public class TimeRangeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ChartTimeRange timeRange) return value?.ToString();

        var localizationKey = timeRange switch
        {
            ChartTimeRange.Year => "Common.Time.Year",
            ChartTimeRange.Week => "Common.Time.Week",
            ChartTimeRange.Month => "Common.Time.Month",
            ChartTimeRange.Day => "Common.Time.Day",
            _ => "Common.Time.All"
            
        };
        
        return LocalizationService.GetString(localizationKey);

    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Localized Time Range value cannot be converted back."));
    }
}