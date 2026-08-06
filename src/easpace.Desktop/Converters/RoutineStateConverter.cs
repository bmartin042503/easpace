// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Constants;
using easpace.Desktop.Services;

namespace easpace.Desktop.Converters;

public class RoutineStateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RoutineState state)
        {
            return state switch
            {
                RoutineState.Completed => LocalizationService.GetString("Activities.RoutineState.Completed"),
                RoutineState.NotCompleted => LocalizationService.GetString("Activities.RoutineState.NotCompleted"),
                RoutineState.None => LocalizationService.GetString("Activities.RoutineState.None"),
                _ => string.Empty
            };
        }

        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Localized Routine State value cannot be converted back."));
    }
}