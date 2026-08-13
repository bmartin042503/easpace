// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace easpace.Desktop.Resources.HavenTheme.TemplatedControls;

public class DataEntryListItem : TemplatedControl
{
    protected override Type StyleKeyOverride => typeof(DataEntryListItem);
    
    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<DataEntryListItem, string>(nameof(Value), defaultValue: string.Empty);
    
    public static readonly StyledProperty<string> TimestampProperty =
        AvaloniaProperty.Register<DataEntryListItem, string>(nameof(Timestamp), defaultValue: string.Empty);
    
    public static readonly StyledProperty<ICommand> EditCommandProperty =
        AvaloniaProperty.Register<DataEntryListItem, ICommand>(nameof(EditCommand));
    
    public static readonly StyledProperty<ICommand> DeleteCommandProperty =
        AvaloniaProperty.Register<DataEntryListItem, ICommand>(nameof(DeleteCommand));
    
    public static readonly StyledProperty<object?> CommandsParameterProperty =
        AvaloniaProperty.Register<DataEntryListItem, object?>(nameof(CommandsParameter));
    
    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    public string Timestamp
    {
        get => GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }

    public ICommand EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand DeleteCommand
    {
        get => GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public object? CommandsParameter
    {
        get => GetValue(CommandsParameterProperty);
        set => SetValue(CommandsParameterProperty, value);
    }
}