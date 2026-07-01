using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace easpace.Desktop.Styles.TemplatedControls;

public class ActivityListItem : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ActivityListItem, string?>(nameof(Title));
    
    public static readonly DirectProperty<ActivityListItem, Guid?> IdProperty =
        AvaloniaProperty.RegisterDirect<ActivityListItem, Guid?>(
            nameof(Id),
            o => o.Id,
            (o, v) => o.Id = v,
            unsetValue: Guid.Empty
        );
    
    public static readonly DirectProperty<ActivityListItem, Guid?> SelectedIdProperty =
        AvaloniaProperty.RegisterDirect<ActivityListItem, Guid?>(
            nameof(SelectedId),
            o => o.SelectedId,
            (o, v) => o.SelectedId = v,
            unsetValue: Guid.Empty
        );
    
    public static readonly StyledProperty<ICommand?> SelectCommandProperty =
        AvaloniaProperty.Register<ActivityListItem, ICommand?>(nameof(SelectCommand));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public Guid? Id
    {
        get;
        set => SetAndRaise(IdProperty, ref field, value);
    }

    public Guid? SelectedId
    {
        get;
        set => SetAndRaise(SelectedIdProperty, ref field, value);
    }

    public ICommand? SelectCommand
    {
        get => GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        switch (change.Property.Name)
        {
            case nameof(SelectedId):
                if (SelectedId.HasValue && Id.HasValue)
                {
                    if (SelectedId.Value == Id.Value)
                    {
                        Classes.Add("selected");
                    }
                    else
                    {
                        Classes.Remove("selected");
                    }
                }
                break;
        }
    }
}
