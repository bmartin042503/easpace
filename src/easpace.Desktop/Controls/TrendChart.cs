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
using easpace.Desktop.Models.Activities;
using easpace.Desktop.Services;

namespace easpace.Desktop.Controls;

public class TrendChart : Control
{
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<IBrush?> AreaBrushProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(AreaBrush));

    public static readonly StyledProperty<IBrush?> AxisStrokeProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(AxisStroke));

    public static readonly StyledProperty<double?> StrokeThicknessProperty =
        AvaloniaProperty.Register<TrendChart, double?>(nameof(StrokeThickness));

    public static readonly StyledProperty<double?> PaddingProperty =
        AvaloniaProperty.Register<TrendChart, double?>(nameof(Padding));

    public static readonly StyledProperty<int?> TicksProperty =
        AvaloniaProperty.Register<TrendChart, int?>(nameof(Ticks));

    public static readonly StyledProperty<double?> TickWidthProperty =
        AvaloniaProperty.Register<TrendChart, double?>(nameof(TickWidth));
    
    public static readonly DirectProperty<TrendChart, double?> TargetProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, double?>(
            nameof(Target),
            o => o.Target,
            (o, v) => o.Target = v,
            0.0
        );

    public static readonly DirectProperty<TrendChart, IEnumerable<NumericDataEntry>?> EntriesProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, IEnumerable<NumericDataEntry>?>(
            nameof(Entries),
            o => o.Entries,
            (o, v) => o.Entries = v,
            []
        );
    
    public static readonly DirectProperty<TrendChart, string?> UnitProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, string?>(
            nameof(Unit),
            o => o.Unit,
            (o, v) => o.Unit = v,
            string.Empty
        );

    static TrendChart()
    {
        AffectsRender<TrendChart>(
            StrokeProperty,
            AreaBrushProperty,
            AxisStrokeProperty,
            StrokeThicknessProperty,
            PaddingProperty,
            TicksProperty,
            TickWidthProperty
        );
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public IBrush? AreaBrush
    {
        get => GetValue(AreaBrushProperty);
        set => SetValue(AreaBrushProperty, value);
    }

    public IBrush? AxisStroke
    {
        get => GetValue(AxisStrokeProperty);
        set => SetValue(AxisStrokeProperty, value);
    }

    public double? StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
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

    public IEnumerable<NumericDataEntry>? Entries
    {
        get;
        set
        {
            if (SetAndRaise(EntriesProperty, ref field, value)) InvalidateVisual();
        }
    }
    
    public double? Target
    {
        get;
        set
        {
            if (SetAndRaise(TargetProperty, ref field, value)) InvalidateVisual();
        }
    }
    
    public string? Unit
    {
        get;
        set
        {
            if (SetAndRaise(UnitProperty, ref field, value)) InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        if (Entries == null || !Entries.Any()) return;

        var padding = Padding ?? 12;
        var ticksCount = Ticks ?? 3;
        var tickWidth = TickWidth ?? 10;
        var strokeThickness = StrokeThickness ?? 2.0;
        var axisPen = new Pen(AxisStroke, strokeThickness);
        
        var exactMin = Entries.Min(e => e.Value);
        var exactMax = Entries.Max(e => e.Value);

        if (Target.HasValue)
        {
            exactMin = Math.Min(exactMin, Target.Value);
            exactMax = Math.Max(exactMax, Target.Value);
        }
        
        var niceInterval = CalculateNiceInterval(exactMax - exactMin, ticksCount);
        
        var graphMax = Math.Ceiling(exactMax / niceInterval) * niceInterval;
        var graphMin = graphMax - (ticksCount - 1) * niceInterval;
        var valueRange = graphMax - graphMin;

        var graphHeight = Bounds.Height - padding * 2;

        var tickTexts = new List<(double Value, TextLayout Layout)>();
        var maxTextWidth = 0.0;

        for (var i = 0; i < ticksCount; i++)
        {
            var val = graphMax - i * niceInterval;
            var textLayout = new TextLayout(
                val.ToString("0.#"),
                new Typeface(_fontFamily),
                12,
                AxisStroke,
                TextAlignment.Right);
            
            tickTexts.Add((val, textLayout));
            maxTextWidth = Math.Max(maxTextWidth, textLayout.Width);
        }

        var textRightMargin = padding + maxTextWidth + 6;
        var axisLineX = textRightMargin + tickWidth / 2;
        var tickEnd = textRightMargin + tickWidth;

        var graphWidth = Bounds.Width - axisLineX - padding;
        var graphArea = new Rect(axisLineX, padding, graphWidth, graphHeight);

        var yTickPadding = 20.0;
        var drawableHeight = graphArea.Height - yTickPadding * 2;
        
        RenderChart(context, graphArea, Entries.ToList(), graphMin, valueRange, yTickPadding, drawableHeight);

        if (Target.HasValue && !string.IsNullOrEmpty(Unit))
        {
            var yRatio = (Target.Value - graphMin) / valueRange;
            var targetY = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;
            
            context.DrawLine(axisPen, new Point(graphArea.Left, targetY), new Point(graphArea.Right, targetY));

            var targetText = new TextLayout(
                $"{LocalizationService.GetString("TARGET")}: {Target.Value} {Unit}",
                new Typeface(_fontFamily),
                12,
                axisPen.Brush
            );
            
            targetText.Draw(context, new Point(graphArea.Left + 4, targetY - targetText.Height - 2));
        }

        foreach (var tick in tickTexts)
        {
            var yRatio = (tick.Value - graphMin) / valueRange;
            var y = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;
            
            tick.Layout.Draw(context, new Point(padding, y - tick.Layout.Height / 2));
            
            context.DrawLine(axisPen, new Point(textRightMargin, y), new Point(tickEnd, y));
        }
        
        context.DrawLine(axisPen, new Point(axisLineX, graphArea.Top), new Point(axisLineX, graphArea.Bottom));
    }

    private readonly FontFamily _fontFamily = new("Poppins");

    private void RenderChart(
        DrawingContext context, 
        Rect graphArea, 
        List<NumericDataEntry> entries, 
        double graphMin, 
        double valueRange,
        double yTickPadding,
        double drawableHeight)
    {
        var minTime = entries.First().Date.Ticks;
        var maxTime = entries.Last().Date.Ticks;
        var timeRange = maxTime - minTime;

        if (timeRange == 0 || valueRange == 0) return;

        var fillGeometry = new StreamGeometry();
        var strokeGeometry = new StreamGeometry();
        
        using (var fillContext = fillGeometry.Open())
        using (var strokeContext = strokeGeometry.Open())
        {
            var isFirstPoint = true;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                
                var xRatio = (double)(entry.Date.Ticks - minTime) / timeRange;
                var x = graphArea.X + xRatio * graphArea.Width;
                
                var yRatio = (entry.Value - graphMin) / valueRange;
                var y = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;
                
                var currentPoint = new Point(x, y);

                if (isFirstPoint)
                {
                    fillContext.BeginFigure(new Point(x, graphArea.Bottom));
                    fillContext.LineTo(currentPoint);
                    
                    strokeContext.BeginFigure(currentPoint, false);
                    
                    isFirstPoint = false;
                }
                else
                {
                    fillContext.LineTo(currentPoint);
                    strokeContext.LineTo(currentPoint);
                }

                if (i == entries.Count - 1)
                {
                    fillContext.LineTo(new Point(x, graphArea.Bottom));
                }
            }
        }
        
        context.DrawGeometry(AreaBrush, null, fillGeometry);

        var linePen = new Pen(Stroke, StrokeThickness ?? 6)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        
        context.DrawGeometry(null, linePen, strokeGeometry);
    }

    private static double CalculateNiceInterval(double exactRange, int ticks)
    {
        var exactInterval = exactRange / (ticks - 1);
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(exactInterval)));
        var fraction = exactInterval / magnitude;
        
        var niceFraction = fraction switch
        {
            <= 1.0 => 1.0,
            <= 2.0 => 2.0,
            <= 5.0 => 5.0,
            _ => 10.0
        };

        return niceFraction * magnitude;
    }
}