// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia;

namespace easpace.Desktop.Resources.HavenTheme.TemplatedControls;

public class NavigationListItem : IconListItem
{
    protected override Type StyleKeyOverride => typeof(NavigationListItem);
    
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<NavigationListItem, string>(nameof(Text), defaultValue: string.Empty);
    
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}