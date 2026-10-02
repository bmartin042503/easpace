// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Mood.Constants;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Converters;

public class EnumToLocalizedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ActivityType activityType => activityType switch
            {
                ActivityType.Trend => LocalizationService.GetString("Activities.Type.Trend"),
                ActivityType.Milestone => LocalizationService.GetString("Activities.Type.Milestone"),
                ActivityType.Routine => LocalizationService.GetString("Activities.Type.Routine"),
                _ => string.Empty
            },
            
            RoutineState routineState => routineState switch
            {
                RoutineState.Completed => LocalizationService.GetString("RoutineActivity.EntryState.Completed"),
                RoutineState.NotCompleted => LocalizationService.GetString("RoutineActivity.EntryState.NotCompleted"),
                RoutineState.None => LocalizationService.GetString("RoutineActivity.EntryState.None"),
                _ => string.Empty
            },
            
            MoodLabelState moodLabelState => LocalizationService.GetString($"Mood.Label.{moodLabelState.ToString()}"),
            
            TrendAggregation aggregation => aggregation switch
            {
                TrendAggregation.Sum => LocalizationService.GetString("Activities.Aggregation.Sum"),
                TrendAggregation.Average => LocalizationService.GetString("Activities.Aggregation.Average"),
                TrendAggregation.Latest => LocalizationService.GetString("Activities.Aggregation.Latest"),
                TrendAggregation.Maximum => LocalizationService.GetString("Activities.Aggregation.Maximum"),
                _ => string.Empty
            },
            
            ChartTimeRange timeRange => timeRange switch
            {
                ChartTimeRange.Year => LocalizationService.GetString("Common.Time.Year"),
                ChartTimeRange.Week => LocalizationService.GetString("Common.Time.Week"),
                ChartTimeRange.Month => LocalizationService.GetString("Common.Time.Month"),
                ChartTimeRange.Day => LocalizationService.GetString("Common.Time.Day"),
                _ => LocalizationService.GetString("Common.Time.All"),
            },
            
            _ => string.Empty
        };
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("Localized values cannot be converted back to enum types."));
    }
}