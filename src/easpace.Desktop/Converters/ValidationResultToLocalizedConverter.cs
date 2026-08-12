// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Services;

namespace easpace.Desktop.Converters;

// Since ValidationAttribute requires compile-time values this converter is needed to localize validation messages
public class ValidationResultToLocalizedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            ValidationResult vr => vr.ErrorMessage,
            _ => value?.ToString()
        };
        
        return string.IsNullOrWhiteSpace(key) ? value : LocalizationService.GetString(key);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Localized values cannot be converted back to keys."));
    }
}