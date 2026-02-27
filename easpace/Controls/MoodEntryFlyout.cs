using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace easpace.Controls;

public class MoodEntryFlyout : PopupFlyoutBase
{
    public static readonly StyledProperty<Control?> ItemsProperty =
        AvaloniaProperty.Register<MoodEntryFlyout, Control?>(nameof(Items));

    [Content]
    public Control? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    protected override Control CreatePresenter()
    {
        var flyoutPresenter = new FlyoutPresenter
        {
            Background = null,
            BorderBrush = null,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Content = Items
        };
        
        flyoutPresenter.Classes.Add("moodEntryFlyoutPresenter");
        return flyoutPresenter;
    }
}
