// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia;

namespace easpace.Desktop.HavenTheme.Controls;

internal class ActivityListItem : IconListItem
{
    protected override Type StyleKeyOverride => typeof(ActivityListItem);
    
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ActivityListItem, string>(nameof(Title), defaultValue: string.Empty);
    
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<ActivityListItem, string?>(nameof(Value));
    
    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<ActivityListItem, string?>(nameof(Unit));

    public static readonly DirectProperty<ActivityListItem, bool> ShowUnitProperty =
        AvaloniaProperty.RegisterDirect<ActivityListItem, bool>(
            nameof(ShowUnit),
            o => o.ShowUnit);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public bool ShowUnit
    {
        get;
        private set => SetAndRaise(ShowUnitProperty, ref field, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty || change.Property == UnitProperty)
        {
            UpdateShowUnit();
        }
    }

    private void UpdateShowUnit()
    {
        ShowUnit = !string.IsNullOrEmpty(Value) && !string.IsNullOrEmpty(Unit);
    }
}