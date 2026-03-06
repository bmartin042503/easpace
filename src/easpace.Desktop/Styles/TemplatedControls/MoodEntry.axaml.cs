using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace easpace.Desktop.Styles.TemplatedControls;

public class MoodEntry : TemplatedControl
{
    public static readonly StyledProperty<double> MoodSliderValueProperty =
        AvaloniaProperty.Register<MoodEntry, double>(nameof(MoodSliderValue), defaultValue: 0.0);
    
    public static readonly StyledProperty<DateTime> DateProperty =
        AvaloniaProperty.Register<MoodEntry, DateTime>(nameof(Date));
    
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<MoodEntry, string?>(nameof(Description));

    public static readonly DirectProperty<MoodEntry, IList<string>?> LabelsProperty =
        AvaloniaProperty.RegisterDirect<MoodEntry, IList<string>?>(
            nameof(Labels),
            o => o.Labels,
            (o, v) => o.Labels = v
        );
    
    public static readonly StyledProperty<ICommand?> DeleteCommandProperty =
        AvaloniaProperty.Register<MoodEntry, ICommand?>(nameof(DeleteCommand));

    public double MoodSliderValue
    {
        get => GetValue(MoodSliderValueProperty);
        set => SetValue(MoodSliderValueProperty, value);
    }

    public DateTime Date
    {
        get => GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public IList<string>? Labels
    {
        get;
        set => SetAndRaise(LabelsProperty, ref field, value);
    }
    
    public ICommand? DeleteCommand
    {
        get => GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
}
