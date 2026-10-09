// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Validation;

namespace easpace.Desktop.Converters;

// Localize validation keys while preserving messages from control and binding errors.
public class ValidationResultToLocalizedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ValidationIssue issue)
        {
            return LocalizationService.GetString(issue.MessageKey);
        }

        var message = value switch
        {
            Exception exception => exception.Message,
            string text => text,
            _ => null
        };
        
        return message is null ? value : LocalizationService.GetString(message, message);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Localized values cannot be converted back to keys."));
    }
}
