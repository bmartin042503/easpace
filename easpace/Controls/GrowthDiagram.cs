using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using easpace.Models;

namespace easpace.Controls;

public class GrowthDiagram : Control
{
    public static readonly StyledProperty<IBrush?> DataLineBrushProperty =
        AvaloniaProperty.Register<GrowthDiagram, IBrush?>(nameof(DataLineBrush));

    public static readonly StyledProperty<IBrush?> DataLineBackgroundProperty =
        AvaloniaProperty.Register<GrowthDiagram, IBrush?>(nameof(DataLineBackground));

    public static readonly StyledProperty<IBrush?> AxisLineBrushProperty =
        AvaloniaProperty.Register<GrowthDiagram, IBrush?>(nameof(AxisLineBrush));

    public static readonly StyledProperty<double?> LineThicknessProperty =
        AvaloniaProperty.Register<GrowthDiagram, double?>(nameof(LineThickness));
    
    public static readonly StyledProperty<double?> PaddingProperty =
        AvaloniaProperty.Register<GrowthDiagram, double?>(nameof(Padding));
    
    public static readonly StyledProperty<int?> TicksProperty =
        AvaloniaProperty.Register<GrowthDiagram, int?>(nameof(Ticks));

    public static readonly DirectProperty<GrowthDiagram, IEnumerable<IGrowthTargetEntry>?> EntriesProperty =
        AvaloniaProperty.RegisterDirect<GrowthDiagram, IEnumerable<IGrowthTargetEntry>?>(
            nameof(Entries),
            o => o.Entries,
            (o, v) => o.Entries = v,
            []
        );

    public IBrush? DataLineBrush
    {
        get => GetValue(DataLineBrushProperty);
        set => SetValue(DataLineBrushProperty, value);
    }

    public IBrush? DataLineBackground
    {
        get => GetValue(DataLineBackgroundProperty);
        set => SetValue(DataLineBackgroundProperty, value);
    }

    public IBrush? AxisLineBrush
    {
        get => GetValue(AxisLineBrushProperty);
        set => SetValue(AxisLineBrushProperty, value);
    }

    public double? LineThickness
    {
        get => GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }
    
    public int? Ticks
    {
        get => GetValue(TicksProperty);
        set => SetValue(TicksProperty, value);
    }
    
    public double? Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public IEnumerable<IGrowthTargetEntry>? Entries
    {
        get;
        set => SetAndRaise(EntriesProperty, ref field, value);
    }

    public override void Render(DrawingContext context)
    {
        RenderDiagramLine(context);
        RenderEntries(context);
    }

    private int _entriesAvg;
    private readonly FontFamily _fontFamily = new("avares://easpace/Assets/Fonts/Poppins");
    private TextLayout?[] _tickTextLayouts = new TextLayout?[3];
    private const double TickWidth = 10;
    private double _axisStartPosX;

    private void RenderDiagramLine(DrawingContext context)
    {
        if (Entries is null) return;
        _entriesAvg = (int)Entries.Average(entry => Convert.ToDouble(entry.Value));
        var lineThickness = LineThickness ?? 6;
        var pen = new Pen(AxisLineBrush ?? new ImmutableSolidColorBrush(Colors.Black), lineThickness);
        
        var maxTickValue = (int)(_entriesAvg + Entries.Max(entry => Convert.ToDouble(entry.Value)) / 2);
        var lowTickValue = (int)(_entriesAvg - Entries.Min(entry => Convert.ToDouble(entry.Value)) / 2);

        _tickTextLayouts[0] = CreateTickTextLayout(maxTickValue.ToString(CultureInfo.InvariantCulture));
        _tickTextLayouts[1] = CreateTickTextLayout(_entriesAvg.ToString(CultureInfo.InvariantCulture));
        _tickTextLayouts[2] = CreateTickTextLayout(lowTickValue.ToString(CultureInfo.InvariantCulture));

        var maxTextWidth = Math.Max(_tickTextLayouts[0]?.Width ?? 0.0,
            Math.Max(_tickTextLayouts[1]?.Width ?? 0.0, _tickTextLayouts[2]?.Width ?? 0.0));

        _axisStartPosX += maxTextWidth;
        _axisStartPosX += TickWidth / 2;
        _axisStartPosX += 6;

        context.DrawLine(pen, new Point(_axisStartPosX, 0), new Point(_axisStartPosX, DesiredSize.Height));

        var tickCount = Ticks ?? 3;
        var tickPosX = _axisStartPosX - TickWidth / 2;
        for (var i = 0; i < tickCount; i++)
        {
            context.DrawLine(pen, new Point(tickPosX, DesiredSize.Height / 2),
                new Point(_axisStartPosX + TickWidth / 2, DesiredSize.Height / 2));
        }

        // middle tick
        context.DrawLine(pen, new Point(_axisStartPosX - TickWidth / 2, DesiredSize.Height / 2),
            new Point(_axisStartPosX + TickWidth / 2, DesiredSize.Height / 2));
        
        // top tick
        context.DrawLine(pen, new Point(_axisStartPosX - TickWidth / 2, DesiredSize.Height / 6),
            new Point(_axisStartPosX + TickWidth / 2, DesiredSize.Height / 6));
        
        // bottom tick
        context.DrawLine(pen, new Point(_axisStartPosX - TickWidth / 2, DesiredSize.Height - DesiredSize.Height / 6),
            new Point(_axisStartPosX + TickWidth / 2, DesiredSize.Height - DesiredSize.Height / 6));

        _tickTextLayouts[1]?.Draw(
            context, 
            new Point(0, DesiredSize.Height / 2 - (_tickTextLayouts[1]?.Height ?? 0.0 - lineThickness) / 2));
        
        _tickTextLayouts[0]?.Draw(
            context, 
            new Point(0, DesiredSize.Height / 6 - (_tickTextLayouts[0]?.Height ?? 0.0 - lineThickness) / 2));
        
        _tickTextLayouts[2]?.Draw(context, 
            new Point(0, DesiredSize.Height - DesiredSize.Height / 6 - (_tickTextLayouts[2]?.Height ?? 0.0 - lineThickness) / 2));
    }

    private void RenderEntries(DrawingContext context)
    {
        if (Entries is null || !Entries.Any()) return;

        var padding = Padding ?? 12;

        var entriesList = Entries.OrderBy(entry => entry.Date).ToList();
        
        var firstDateTime = entriesList.First().Date.Ticks;
        var lastDateTime = entriesList.Last().Date.Ticks;
        var totalDuration = lastDateTime - firstDateTime;

        var maxUnit = (double)entriesList.MaxBy(entry => Convert.ToDouble(entry.Value)).Value;
        var minUnit = (double)entriesList.MinBy(entry => Convert.ToDouble(entry.Value)).Value;
        var diff = Math.Abs(maxUnit - minUnit);

        var lineStartPoint = new Point();

        for (var i = 0; i < entriesList.Count; i++)
        {
            var offsetX = entriesList[i].Date.Ticks - firstDateTime;
            var ratioX = offsetX / (double)totalDuration;
            var posX = _axisStartPosX + ratioX * (DesiredSize.Width - _axisStartPosX);
            
            var offsetY = maxUnit - (double)entriesList[i].Value;
            var ratioY = offsetY / diff;
            var posY = padding + ratioY * (DesiredSize.Height - padding * 2);
            
            var dataLineBrush = DataLineBrush ?? new ImmutableSolidColorBrush(Colors.Black);
            var dataPoint = new Point(posX, posY);
            context.DrawEllipse(dataLineBrush, null, dataPoint, LineThickness * 1.5 ?? 9, LineThickness * 1.5 ?? 9);
            var ellipseTextLayout = CreateTickTextLayout(((double)entriesList[i].Value).ToString(CultureInfo.InvariantCulture));
            ellipseTextLayout?.Draw(context, new Point(dataPoint.X, dataPoint.Y + 16));
            
            if (i > 0)
            {
                var pen = new Pen(DataLineBrush ?? new ImmutableSolidColorBrush(Colors.Black), LineThickness ?? 6);
                context.DrawLine(pen, lineStartPoint, dataPoint);
            }
            lineStartPoint = dataPoint; 
        }
    }

    private TextLayout? CreateTickTextLayout(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        return new TextLayout(
            text,
            new Typeface(_fontFamily),
            null,
            12,
            AxisLineBrush ?? new ImmutableSolidColorBrush(Colors.Black),
            TextAlignment.Right,
            TextWrapping.NoWrap,
            TextTrimming.None
        );
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return availableSize;
    }

    protected override void OnMeasureInvalidated()
    {
        base.OnMeasureInvalidated();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
    }
}
