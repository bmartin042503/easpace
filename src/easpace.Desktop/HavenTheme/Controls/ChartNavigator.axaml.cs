// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace easpace.Desktop.HavenTheme.Controls;

public class ChartNavigator : TemplatedControl
{
    public static readonly StyledProperty<string> IntervalTextProperty =
        AvaloniaProperty.Register<ActivityListItem, string>(nameof(IntervalText), string.Empty);
    
    public static readonly StyledProperty<ICommand> NavigateBackCommandProperty =
        AvaloniaProperty.Register<DataEntryListItem, ICommand>(nameof(NavigateBackCommand));
    
    public static readonly StyledProperty<ICommand> NavigateForwardCommandProperty =
        AvaloniaProperty.Register<DataEntryListItem, ICommand>(nameof(NavigateForwardCommand));

    public string IntervalText
    {
        get => GetValue(IntervalTextProperty);
        set => SetValue(IntervalTextProperty, value);
    }
    
    public ICommand NavigateBackCommand
    {
        get => GetValue(NavigateBackCommandProperty);
        set => SetValue(NavigateBackCommandProperty, value);
    }

    public ICommand NavigateForwardCommand
    {
        get => GetValue(NavigateForwardCommandProperty);
        set => SetValue(NavigateForwardCommandProperty, value);
    }
}