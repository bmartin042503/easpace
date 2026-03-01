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

    public static readonly StyledProperty<double?> TickWidthProperty =
        AvaloniaProperty.Register<GrowthDiagram, double?>(nameof(TickWidth));

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

    public double? Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public int? Ticks
    {
        get => GetValue(TicksProperty);
        set => SetValue(TicksProperty, value);
    }

    public double? TickWidth
    {
        get => GetValue(TickWidthProperty);
        set => SetValue(TickWidthProperty, value);
    }

    public IEnumerable<IGrowthTargetEntry>? Entries
    {
        get;
        set => SetAndRaise(EntriesProperty, ref field, value);
    }

    public GrowthDiagram()
    {
        _tickTextLayouts = new List<TextLayout>();
    }

    public override void Render(DrawingContext context)
    {
        _renderPosX = 0;
        RenderDiagramLine(context);
        RenderEntries(context);
    }

    private readonly FontFamily _fontFamily = new("avares://easpace/Assets/Fonts/Poppins");
    private IList<TextLayout?> _tickTextLayouts;
    private double _renderPosX;

    private void RenderDiagramLine(DrawingContext context)
    {
        if (Entries is null) return;

        var lineThickness = LineThickness ?? 6;
        var ticks = Ticks ?? 3;
        var tickWidth = TickWidth ?? 10;
        var padding = Padding ?? 12;
        var pen = new Pen(AxisLineBrush ?? new ImmutableSolidColorBrush(Colors.Black), lineThickness);

        var tickDistance = (DesiredSize.Height - padding * 2) / (ticks - 1);
        var tickTextMaxWidth = 0.0;

        var entriesMax = Convert.ToDouble(Entries.MaxBy(entry => entry.Value)?.Value);
        var entriesMin = Convert.ToDouble(Entries.MinBy(entry => entry.Value)?.Value);

        // render ticks' text
        for (var i = 0; i < ticks; i++)
        {
            var tickStartPointY = tickDistance * i + padding;

            var tickValue = GetValueFromPosY(tickStartPointY);
            
            var tickTextLayout = CreateTickTextLayout(tickValue.ToString(CultureInfo.InvariantCulture));
            
            var tickTextStartPoint = new Point(
                _renderPosX,
                tickStartPointY - (tickTextLayout.Height - lineThickness) / 2);
            
            tickTextLayout.Draw(context, tickTextStartPoint);

            if (tickTextLayout.Width > tickTextMaxWidth) tickTextMaxWidth = tickTextLayout.Width;

            _tickTextLayouts.Add(tickTextLayout);
        }

        _renderPosX += tickTextMaxWidth;
        _renderPosX += 6;

        // render ticks
        for (var i = 0; i < ticks; i++)
        {
            var tickStartPoint = new Point(_renderPosX, tickDistance * i + padding);
            var tickEndPoint = new Point(_renderPosX + tickWidth, tickDistance * i + padding);
            context.DrawLine(pen, tickStartPoint, tickEndPoint);
        }
        
        _renderPosX += tickWidth / 2;
        
        // render Y axis line
        context.DrawLine(pen, new Point(_renderPosX, 0), new Point(_renderPosX, DesiredSize.Height));
        
        _renderPosX += tickWidth / 2;
    }

    private void RenderEntries(DrawingContext context)
    {
        if (Entries is null || !Entries.Any()) return;

        var padding = Padding ?? 12;

        var entriesList = Entries.OrderBy(entry => entry.Date).ToList();

        var firstDateTime = entriesList.First().Date.Ticks;
        var lastDateTime = entriesList.Last().Date.Ticks;
        var totalDuration = lastDateTime - firstDateTime;

        var entriesMax = Convert.ToDouble(entriesList.MaxBy(entry => entry.Value)?.Value);
        var entriesMin = Convert.ToDouble(entriesList.MinBy(entry => entry.Value)?.Value);
        var diff = Math.Abs(entriesMax - entriesMin);

        var lineStartPoint = new Point();

        for (var i = 0; i < entriesList.Count; i++)
        {
            var offsetX = entriesList[i].Date.Ticks - firstDateTime;
            var ratioX = offsetX / (double)totalDuration;
            var posX = _renderPosX + ratioX * (DesiredSize.Width - _renderPosX);

            var offsetY = entriesMax - Convert.ToDouble(entriesList[i].Value);
            var ratioY = offsetY / diff;
            var posY = padding + ratioY * (DesiredSize.Height - padding * 2);

            var dataLineBrush = DataLineBrush ?? new ImmutableSolidColorBrush(Colors.Black);
            var dataPoint = new Point(posX, posY);
            context.DrawEllipse(dataLineBrush, null, dataPoint, LineThickness * 1.3 ?? 9, LineThickness * 1.3 ?? 9);
            var ellipseTextLayout =
                CreateTickTextLayout(Convert.ToDouble(entriesList[i].Value).ToString(CultureInfo.InvariantCulture));
            ellipseTextLayout.Draw(context, new Point(dataPoint.X, dataPoint.Y + 16));

            if (i > 0)
            {
                var pen = new Pen(DataLineBrush ?? new ImmutableSolidColorBrush(Colors.Black), LineThickness ?? 6);
                context.DrawLine(pen, lineStartPoint, dataPoint);
            }

            lineStartPoint = dataPoint;
        }
    }

    private double GetValueFromPosY(double y)
    {
        if (Entries is null || !Entries.Any()) return 0.0;
        var padding = Padding ?? 12;
        var entriesMax = Convert.ToDouble(Entries.MaxBy(entry => entry.Value)?.Value);
        var entriesMin = Convert.ToDouble(Entries.MinBy(entry => entry.Value)?.Value);
        var diff = Math.Abs(entriesMax - entriesMin);
        
        var diagramHeight = DesiredSize.Height - padding * 2;

        if (diagramHeight <= 0) return 0.0;

        var offsetY = y - padding;
        var ratioY = offsetY / diagramHeight;

        return entriesMax - ratioY * diff;
    }

    private TextLayout CreateTickTextLayout(string text)
    {
        return new TextLayout(
            string.IsNullOrEmpty(text) ? "Tick" : text,
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
