// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia;

namespace easpace.Desktop.Resources.HavenTheme.TemplatedControls;

internal class ActivityListItem : IconListItem
{
    protected override Type StyleKeyOverride => typeof(ActivityListItem);
    
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ActivityListItem, string>(nameof(Title), defaultValue: string.Empty);
    
    public static readonly StyledProperty<string> StatusProperty =
        AvaloniaProperty.Register<ActivityListItem, string>(nameof(Status), defaultValue: string.Empty);
    
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }
}