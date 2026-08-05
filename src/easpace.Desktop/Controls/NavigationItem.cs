// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace easpace.Desktop.Controls;

public class NavigationItem : ListBoxItem
{
    public static readonly StyledProperty<StreamGeometry> IconProperty =
        AvaloniaProperty.Register<NavigationItem, StreamGeometry>(nameof(Icon));
    
    public static readonly StyledProperty<StreamGeometry> FilledIconProperty =
        AvaloniaProperty.Register<NavigationItem, StreamGeometry>(nameof(FilledIcon));
    
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<NavigationItem, string>(nameof(Text), defaultValue: string.Empty);

    public StreamGeometry Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public StreamGeometry FilledIcon
    {
        get => GetValue(FilledIconProperty);
        set => SetValue(FilledIconProperty, value);
    }
    
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}