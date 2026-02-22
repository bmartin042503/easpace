using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Services;

namespace easpace.Converters;

public class MoodSliderValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double sliderValue) return null;
        var localizedValue = sliderValue switch
        {
            < 0.2 => LocalizationService.GetString("VERY_UNPLEASANT"),
            < 0.4 => LocalizationService.GetString("SLIGHTLY_UNPLEASANT"),
            < 0.6 => LocalizationService.GetString("NEUTRAL"),
            < 0.8 => LocalizationService.GetString("SLIGHTLY_PLEASANT"),
            <= 1.0 => LocalizationService.GetString("VERY_PLEASANT"),
            _ => string.Empty
        };

        return localizedValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Mood value cannot be converted back."));
    }
}
