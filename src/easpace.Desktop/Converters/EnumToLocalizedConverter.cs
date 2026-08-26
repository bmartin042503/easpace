// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Mood.Constants;
using easpace.Desktop.Features.Wellness.Constants;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Converters;

public class EnumToLocalizedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ActivityType activityType)
        {
            return activityType switch
            {
                ActivityType.Trend => LocalizationService.GetString("Activities.Type.Trend"),
                ActivityType.Milestone => LocalizationService.GetString("Activities.Type.Milestone"),
                ActivityType.Routine => LocalizationService.GetString("Activities.Type.Routine"),
                _ => string.Empty
            };
        }
        
        if (value is RoutineState routineState)
        {
            return routineState switch
            {
                RoutineState.Completed => LocalizationService.GetString("RoutineActivity.EntryState.Completed"),
                RoutineState.NotCompleted => LocalizationService.GetString("RoutineActivity.EntryState.NotCompleted"),
                RoutineState.None => LocalizationService.GetString("RoutineActivity.EntryState.None"),
                _ => string.Empty
            };
        }

        if (value is MoodLabelState moodLabelState)
        {
            return LocalizationService.GetString($"Mood.Label.{moodLabelState.ToString()}");
        }

        if (value is WellnessSessionType sessionType)
        {
            return sessionType switch
            {
                WellnessSessionType.Breathing => LocalizationService.GetString("Wellness.SessionType.Breathing"),
                WellnessSessionType.Meditation => LocalizationService.GetString("Wellness.SessionType.Meditation"),
                _ => string.Empty
            };
        }
        
        return string.Empty;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Localized values cannot be converted back to enum types."));
    }
}