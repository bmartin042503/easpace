// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Services;

namespace easpace.Desktop.Features.Activities.Controls;

public class TrendChart : Control
{
    #region Fields

    private readonly FontFamily _fontFamily = new("avares://easpace.Desktop/Assets/Fonts#Poppins");
    private readonly List<(Point CanvasPoint, TrendChartDataPoint DataPoint)> _dataPoints = [];
    private TrendChartDataPoint? _hoveredDataPoint;
    private Point _hoveredPoint;

    #endregion

    #region Styled & Direct Properties

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<IBrush?> GridLineBrushProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(GridLineBrush));

    public static readonly StyledProperty<int?> GridLinesProperty =
        AvaloniaProperty.Register<TrendChart, int?>(nameof(GridLines));

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

    public static readonly StyledProperty<IBrush?> TooltipBackgroundProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(TooltipBackground));

    public static readonly StyledProperty<IBrush?> TooltipValueForegroundProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(TooltipValueForeground));

    public static readonly StyledProperty<IBrush?> TooltipDateForegroundProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(TooltipDateForeground));

    public static readonly StyledProperty<IBrush?> TooltipBorderBrushProperty =
        AvaloniaProperty.Register<TrendChart, IBrush?>(nameof(TooltipBorderBrush));

    public static readonly StyledProperty<double> TooltipBorderThicknessProperty =
        AvaloniaProperty.Register<TrendChart, double>(nameof(TooltipBorderThickness), 1.0);

    public static readonly DirectProperty<TrendChart, double?> TargetProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, double?>(
            nameof(Target),
            o => o.Target,
            (o, v) => o.Target = v,
            0.0
        );

    public static readonly DirectProperty<TrendChart, IEnumerable<TrendChartDataPoint>?> DataPointsProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, IEnumerable<TrendChartDataPoint>?>(
            nameof(DataPoints),
            o => o.DataPoints,
            (o, v) => o.DataPoints = v,
            []
        );

    public static readonly DirectProperty<TrendChart, string?> UnitProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, string?>(
            nameof(Unit),
            o => o.Unit,
            (o, v) => o.Unit = v,
            string.Empty
        );

    #endregion

    #region Properties

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

    public IBrush? GridLineBrush
    {
        get => GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    public int? GridLines
    {
        get => GetValue(GridLinesProperty);
        set => SetValue(GridLinesProperty, value);
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

    public IBrush? TooltipBackground
    {
        get => GetValue(TooltipBackgroundProperty);
        set => SetValue(TooltipBackgroundProperty, value);
    }

    public IBrush? TooltipValueForeground
    {
        get => GetValue(TooltipValueForegroundProperty);
        set => SetValue(TooltipValueForegroundProperty, value);
    }

    public IBrush? TooltipDateForeground
    {
        get => GetValue(TooltipDateForegroundProperty);
        set => SetValue(TooltipDateForegroundProperty, value);
    }

    public IBrush? TooltipBorderBrush
    {
        get => GetValue(TooltipBorderBrushProperty);
        set => SetValue(TooltipBorderBrushProperty, value);
    }

    public double TooltipBorderThickness
    {
        get => GetValue(TooltipBorderThicknessProperty);
        set => SetValue(TooltipBorderThicknessProperty, value);
    }

    public IEnumerable<TrendChartDataPoint>? DataPoints
    {
        get;
        set
        {
            if (SetAndRaise(DataPointsProperty, ref field, value)) InvalidateVisual();
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

    #endregion

    #region Initialization

    static TrendChart()
    {
        AffectsRender<TrendChart>(
            StrokeProperty,
            AreaBrushProperty,
            GridLineBrushProperty,
            GridLinesProperty,
            AxisStrokeProperty,
            StrokeThicknessProperty,
            PaddingProperty,
            TicksProperty,
            TickWidthProperty,
            TooltipBackgroundProperty,
            TooltipValueForegroundProperty,
            TooltipDateForegroundProperty,
            TooltipBorderBrushProperty,
            TooltipBorderThicknessProperty
        );
    }

    #endregion

    #region Rendering

    public override void Render(DrawingContext context)
    {
        // create a safe list and check if there is any data
        var dataPoints = DataPoints?.ToList() ?? [];
        var hasDataPoints = dataPoints.Count > 0;
        
        // base values
        var padding = Padding ?? 12;
        var ticksCount = Ticks ?? 3;
        var tickWidth = TickWidth ?? 10;
        var strokeThickness = StrokeThickness ?? 2.0;
        var axisPen = new Pen(AxisStroke, strokeThickness);

        // find min and max values to determine the scale
        double exactMin = 0;
        double exactMax = 100;

        if (hasDataPoints)
        {
            // get real min/max if we have data
            exactMin = dataPoints.Min(e => e.Value);
            exactMax = dataPoints.Max(e => e.Value);
        }
        else if (Target.HasValue)
        {
            // build a default scale around the target if the chart is empty
            exactMin = Target.Value - 10;
            exactMax = Target.Value + 10;
        }
        
        if (Target.HasValue)
        {
            // ensure the target fits inside the calculated boundaries
            exactMin = Math.Min(exactMin, Target.Value);
            exactMax = Math.Max(exactMax, Target.Value);
        }
        
        // add a 10% breathing room (padding) so lines and target don't overlap the chart edges
        if (Math.Abs(exactMin - exactMax) < 0.1)
        {
            // handle straight lines (no variance in data)
            var pad = exactMin == 0 ? 10 : Math.Abs(exactMin * 0.1);
            exactMin -= pad;
            exactMax += pad;
        }
        else
        {
            // standard 10% padding based on value range
            var rangePad = (exactMax - exactMin) * 0.1;
            exactMin -= rangePad;
            exactMax += rangePad;
        }

        // calculate nicely rounded boundaries for the y-axis
        var niceInterval = CalculateNiceInterval(exactMax - exactMin, ticksCount);
        var graphMax = Math.Ceiling(exactMax / niceInterval) * niceInterval;
        var graphMin = graphMax - (ticksCount - 1) * niceInterval;
        var valueRange = graphMax - graphMin;

        // calculate time range for the x-axis
        long timeMin, timeMax;
        
        if (hasDataPoints)
        {
            timeMin = dataPoints.First().Timestamp!.Value.Ticks;
            timeMax = dataPoints.Last().Timestamp!.Value.Ticks;
        }
        else
        {
            // draw a visual 1-day fallback range for empty charts
            timeMax = DateTime.Now.Ticks;
            timeMin = DateTime.Now.AddDays(-1).Ticks;
        }

        var timeRange = timeMax - timeMin;

        // create a fictional time window (+/- 12 hours) if there is only 1 data point
        if (timeRange == 0)
        {
            timeMin -= TimeSpan.FromHours(12).Ticks;
            timeMax += TimeSpan.FromHours(12).Ticks;
            timeRange = timeMax - timeMin;
        }

        // layout measurements
        const double xAxisHeight = 25.0;
        const double yTickPadding = 20.0;
        var graphHeight = Bounds.Height - padding * 2 - xAxisHeight;
        var drawableHeight = graphHeight - yTickPadding * 2;
        
        if (graphHeight <= 0 || drawableHeight <= 0) return;

        // measure y-axis texts to determine left margin
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

        // compute the actual drawing area for the chart data
        var graphWidth = Bounds.Width - axisLineX - padding;
        var graphArea = new Rect(axisLineX, padding, graphWidth, graphHeight);

        // rendering order (z-index): back to front
        
        // 1. draw the data series first so everything else sits on top of it
        if (hasDataPoints)
        {
            DrawDataSeries(context, graphArea, dataPoints, graphMin, valueRange, timeMin, timeRange, yTickPadding, drawableHeight);
        }
        else
        {
            // clear the hit-test points if there are no entries
            _dataPoints.Clear(); 
        }

        // 2. draw grid, axes and target lines over the data
        DrawGridAndXAxis(context, graphArea, axisPen, timeMin, timeRange);
        DrawTargetLine(context, graphArea, axisPen, graphMin, valueRange, yTickPadding, drawableHeight);
        DrawYAxis(context, graphArea, axisPen, tickTexts, graphMin, valueRange, yTickPadding, drawableHeight, padding, textRightMargin, tickEnd, axisLineX);
        
        // 3. draw tooltip absolutely last to always stay on top
        if (hasDataPoints)
        {
            DrawTooltip(context);
        }
    }

    private void DrawGridAndXAxis(
        DrawingContext context, 
        Rect graphArea, 
        Pen axisPen, 
        long timeMin, 
        long timeRange)
    {
        // base x-axis line
        context.DrawLine(axisPen, new Point(graphArea.Left, graphArea.Bottom), new Point(graphArea.Right, graphArea.Bottom));

        var gridLines = GridLines ?? 7;
        var totalDays = new TimeSpan(timeRange).TotalDays;
        var dateFormat = totalDays > 365 ? "yyyy. MM." : "MM. dd.";
        var gridPen = new Pen(GridLineBrush) { DashStyle = DashStyle.Dash };

        for (var i = 0; i < gridLines; i++)
        {
            var ratio = (double)i / (gridLines - 1);
            var x = graphArea.Left + ratio * graphArea.Width;

            // draw vertical grid line (skip the first one to avoid overlapping the y-axis)
            if (i > 0) context.DrawLine(gridPen, new Point(x, graphArea.Top), new Point(x, graphArea.Bottom));

            var tickTicks = timeMin + (long)(timeRange * ratio);
            var tickDate = new DateTime(tickTicks);

            var xText = new TextLayout(
                tickDate.ToString(dateFormat),
                new Typeface(_fontFamily),
                11,
                AxisStroke,
                TextAlignment.Center);

            // align text to prevent clipping on edges
            var textX = x - xText.Width / 2;
            if (i == 0) textX = x;
            if (i == gridLines - 1) textX = x - xText.Width;

            xText.Draw(context, new Point(textX, graphArea.Bottom + 4));
        }
    }

    private static void DrawYAxis(
        DrawingContext context, 
        Rect graphArea, 
        Pen axisPen, 
        List<(double Value, TextLayout Layout)> tickTexts,
        double graphMin, 
        double valueRange, 
        double yTickPadding, 
        double drawableHeight, 
        double padding,
        double textRightMargin,
        double tickEnd,
        double axisLineX)
    {
        // base y-axis line
        context.DrawLine(axisPen, new Point(axisLineX, graphArea.Top), new Point(axisLineX, graphArea.Bottom));

        foreach (var tick in tickTexts)
        {
            // calculate vertical ratio based on value
            var yRatio = (tick.Value - graphMin) / valueRange;
            var y = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;

            tick.Layout.Draw(context, new Point(padding, y - tick.Layout.Height / 2));
            context.DrawLine(axisPen, new Point(textRightMargin, y), new Point(tickEnd, y));
        }
    }

    private void DrawTargetLine(
        DrawingContext context, 
        Rect graphArea, 
        Pen axisPen, 
        double graphMin, 
        double valueRange, 
        double yTickPadding, 
        double drawableHeight)
    {
        if (!Target.HasValue || string.IsNullOrEmpty(Unit)) return;

        var yRatio = (Target.Value - graphMin) / valueRange;
        var targetY = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;

        context.DrawLine(axisPen, new Point(graphArea.Left, targetY), new Point(graphArea.Right, targetY));

        var targetText = new TextLayout(
            $"{LocalizationService.GetString("Activities.NumericActivity.Target")}: {Target.Value} {Unit}",
            new Typeface(_fontFamily),
            12,
            axisPen.Brush
        );

        // prevent the text from clipping at the top of the control
        var textY = targetY - targetText.Height - 2;
        if (textY < graphArea.Top) 
        {
            // move the text below the line if there is no space above
            textY = targetY + 2; 
        }

        targetText.Draw(context, new Point(graphArea.Left + 4, textY));
    }

    private void DrawDataSeries(
        DrawingContext context,
        Rect graphArea,
        List<TrendChartDataPoint> dataPoints,
        double graphMin,
        double valueRange,
        long timeMin,
        long timeRange,
        double yTickPadding,
        double drawableHeight)
    {
        var fillGeometry = new StreamGeometry();
        var strokeGeometry = new StreamGeometry();

        _dataPoints.Clear();

        // build geometric paths for the gradient area and the stroke line
        using (var fillContext = fillGeometry.Open())
        using (var strokeContext = strokeGeometry.Open())
        {
            var isFirstPoint = true;

            for (var i = 0; i < dataPoints.Count; i++)
            {
                var dataPoint = dataPoints[i];

                var xRatio = (double)(dataPoint.Timestamp!.Value.Ticks - timeMin) / timeRange;
                var x = graphArea.X + xRatio * graphArea.Width;

                var yRatio = (dataPoint.Value - graphMin) / valueRange;
                var y = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;

                var currentPoint = new Point(x, y);
                
                // store calculated points for hover hit-testing
                _dataPoints.Add((currentPoint, dataPoint));

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

                if (i == dataPoints.Count - 1)
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

        // draw data markers (dots)
        foreach (var dp in _dataPoints)
        {
            context.DrawEllipse(Stroke, null, dp.CanvasPoint, 3, 3);
        }
    }

    private void DrawTooltip(DrawingContext context)
    {
        if (_hoveredDataPoint == null) return;

        // highlight the hovered data point
        context.DrawEllipse(Stroke, null, _hoveredPoint, 5, 5);

        var valueText = new TextLayout(
            $"{_hoveredDataPoint.Value} {Unit}",
            new Typeface(_fontFamily),
            14,
            TooltipValueForeground,
            TextAlignment.Center);

        var dateText = new TextLayout(
            $"{_hoveredDataPoint.Timestamp:yyyy. MM. dd. hh:mm:ss}",
            new Typeface(_fontFamily),
            10,
            TooltipDateForeground ?? AxisStroke,
            TextAlignment.Center);

        const int paddingX = 8;
        const int paddingY = 6;
        const int spacing = 2;

        var tooltipWidth = Math.Max(valueText.Width, dateText.Width) + paddingX * 2;
        var tooltipHeight = valueText.Height + spacing + dateText.Height + paddingY * 2;

        // center tooltip above the hovered point
        var tooltipX = _hoveredPoint.X - tooltipWidth / 2;
        var tooltipY = _hoveredPoint.Y - tooltipHeight - 12;

        // keep tooltip within control boundaries
        if (tooltipX < 0) tooltipX = 0;
        if (tooltipX + tooltipWidth > Bounds.Width) tooltipX = Bounds.Width - tooltipWidth;
        if (tooltipY < 0) tooltipY = _hoveredPoint.Y + 12;

        var tooltipRect = new Rect(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
        var borderPen = new Pen(TooltipBorderBrush, TooltipBorderThickness);

        context.DrawRectangle(TooltipBackground, borderPen, new RoundedRect(tooltipRect, 6, 6));

        var valueTextX = tooltipX + (tooltipWidth - valueText.Width) / 2;
        var dateTextX = tooltipX + (tooltipWidth - dateText.Width) / 2;

        valueText.Draw(context, new Point(valueTextX, tooltipY + paddingY));
        dateText.Draw(context, new Point(dateTextX, tooltipY + paddingY + valueText.Height + spacing));
    }

    #endregion

    #region Helpers

    private static double CalculateNiceInterval(double exactRange, int ticks)
    {
        var exactInterval = exactRange / (ticks - 1);
        
        // find the closest power of 10 to determine the magnitude of the interval
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(exactInterval)));
        var fraction = exactInterval / magnitude;

        // snap to nice, human-readable numbers
        var niceFraction = fraction switch
        {
            <= 1.0 => 1.0,
            <= 2.0 => 2.0,
            <= 5.0 => 5.0,
            _ => 10.0
        };

        return niceFraction * magnitude;
    }

    #endregion

    #region Interaction

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_dataPoints.Count == 0) return;

        var currentPosition = e.GetPosition(this);
        var closestPoint = _dataPoints.MinBy(dp => Math.Abs(dp.CanvasPoint.X - currentPosition.X));

        if (Math.Abs(closestPoint.CanvasPoint.X - currentPosition.X) < 40)
        {
            if (_hoveredDataPoint != closestPoint.DataPoint)
            {
                _hoveredDataPoint = closestPoint.DataPoint;
                _hoveredPoint = closestPoint.CanvasPoint;
                InvalidateVisual();
            }
        }
        else
        {
            if (_hoveredDataPoint != null)
            {
                _hoveredDataPoint = null;
                InvalidateVisual();
            }
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (_hoveredDataPoint != null)
        {
            _hoveredDataPoint = null;
            InvalidateVisual();
        }
    }

    #endregion
}