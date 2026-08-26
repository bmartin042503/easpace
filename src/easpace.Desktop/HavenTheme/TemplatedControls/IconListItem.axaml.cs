// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace easpace.Desktop.Resources.HavenTheme.TemplatedControls;

internal class IconListItem : TemplatedControl
{
    public static readonly StyledProperty<StreamGeometry> IconProperty =
        AvaloniaProperty.Register<IconListItem, StreamGeometry>(nameof(Icon));
    
    public static readonly StyledProperty<StreamGeometry> SelectedIconProperty =
        AvaloniaProperty.Register<IconListItem, StreamGeometry>(nameof(SelectedIcon));
    
    public StreamGeometry Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public StreamGeometry SelectedIcon
    {
        get => GetValue(SelectedIconProperty);
        set => SetValue(SelectedIconProperty, value);
    }
}