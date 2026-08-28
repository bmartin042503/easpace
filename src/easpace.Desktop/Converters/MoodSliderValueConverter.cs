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
            < 0.125 => LocalizationService.GetString("Mood.State.VeryUnpleasant"),
            < 0.375 => LocalizationService.GetString("Mood.State.SlightlyUnpleasant"),
            < 0.625 => LocalizationService.GetString("Mood.State.Neutral"),
            < 0.875 => LocalizationService.GetString("Mood.State.SlightlyPleasant"),
            _ => LocalizationService.GetString("Mood.State.VeryPleasant")
        };

        return localizedValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Mood value cannot be converted back."));
    }
}
