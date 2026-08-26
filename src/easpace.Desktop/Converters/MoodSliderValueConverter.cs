// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Converters;

public class MoodSliderValueConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double sliderValue) return null;
        var localizedValue = sliderValue switch
        {
            < 0.2 => LocalizationService.GetString("Mood.State.VeryUnpleasant"),
            < 0.4 => LocalizationService.GetString("Mood.State.SlightlyUnpleasant"),
            < 0.6 => LocalizationService.GetString("Mood.State.Neutral"),
            < 0.8 => LocalizationService.GetString("Mood.State.SlightlyPleasant"),
            <= 1.0 => LocalizationService.GetString("Mood.State.VeryPleasant"),
            _ => string.Empty
        };

        return localizedValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Mood value cannot be converted back."));
    }
}
