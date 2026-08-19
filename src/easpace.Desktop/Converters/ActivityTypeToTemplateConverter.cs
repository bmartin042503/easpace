// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Features.Activities.Constants;

namespace easpace.Desktop.Converters;

public class ActivityTypeToTemplateConverter : IValueConverter
{
    public IDataTemplate? TrendTemplate { get; set; }
    public IDataTemplate? MilestoneTemplate { get; set; }
    public IDataTemplate? RoutineTemplate { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ActivityType type)
        {
            return type switch
            {
                ActivityType.Trend => TrendTemplate,
                ActivityType.Milestone => MilestoneTemplate,
                ActivityType.Routine => RoutineTemplate,
                _ => null
            };
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Activity templates cannot be converted back to Activity type."));
    }
}