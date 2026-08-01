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
                RoutineState.Completed => LocalizationService.GetString("ROUTINE_STATE_COMPLETED"),
                RoutineState.NotCompleted => LocalizationService.GetString("ROUTINE_STATE_NOT_COMPLETED"),
                RoutineState.None => LocalizationService.GetString("ROUTINE_STATE_NONE"),
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