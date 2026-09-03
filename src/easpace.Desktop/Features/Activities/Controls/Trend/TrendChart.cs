// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;

namespace easpace.Desktop.Features.Activities.Controls.Trend;

internal partial class TrendChart : Control
{
    #region Fields

    private const double TooltipHitRadius = 40;
    private static readonly TimeSpan DataAnimationDuration = TimeSpan.FromMilliseconds(700);

    private readonly FontFamily _fontFamily = new("avares://easpace.Desktop/Assets/Fonts#Poppins");
    private readonly List<(Point CanvasPoint, TrendChartDataPoint DataPoint)> _dataPoints = [];

    private TrendChartDataPoint? _hoveredDataPoint;
    private Point _hoveredPoint;

    private int _animationVersion;
    private TimeSpan? _animationStartTime;
    private double _animationProgress = 1;

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
            (o, v) => o.Target = v
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

    public static readonly DirectProperty<TrendChart, DateTimeOffset?> RangeStartProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, DateTimeOffset?>(
            nameof(RangeStart),
            o => o.RangeStart,
            (o, v) => o.RangeStart = v);

    public static readonly DirectProperty<TrendChart, DateTimeOffset?> RangeEndProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, DateTimeOffset?>(
            nameof(RangeEnd),
            o => o.RangeEnd,
            (o, v) => o.RangeEnd = v);

    public static readonly DirectProperty<TrendChart, ChartTimeRange> TimeRangeProperty =
        AvaloniaProperty.RegisterDirect<TrendChart, ChartTimeRange>(
            nameof(TimeRange),
            o => o.TimeRange,
            (o, v) => o.TimeRange = v);

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
            var dataPoints = value?.ToList();

            if (!SetAndRaise(DataPointsProperty, ref field, dataPoints)) return;

            _hoveredDataPoint = null;
            _dataPoints.Clear();

            StartDataAnimation(dataPoints?.Count(e => e.Timestamp.HasValue) ?? 0);
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

    public DateTimeOffset? RangeStart
    {
        get;
        set
        {
            if (SetAndRaise(RangeStartProperty, ref field, value)) InvalidateVisual();
        }
    }

    public DateTimeOffset? RangeEnd
    {
        get;
        set
        {
            if (SetAndRaise(RangeEndProperty, ref field, value)) InvalidateVisual();
        }
    }

    public ChartTimeRange TimeRange
    {
        get;
        set
        {
            if (SetAndRaise(TimeRangeProperty, ref field, value)) InvalidateVisual();
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
            TooltipBorderThicknessProperty,
            DataPointsProperty,
            RangeStartProperty,
            RangeEndProperty,
            TimeRangeProperty
        );
    }

    #endregion

    #region Rendering

    public override void Render(DrawingContext context)
    {
        // create a safe, ordered list and check if there is any data
        var dataPoints = DataPoints?
            .Where(e => e.Timestamp.HasValue)
            .OrderBy(e => e.Timestamp)
            .ToList() ?? [];
        var hasDataPoints = dataPoints.Count > 0;

        // base values
        var padding = Padding ?? 12;
        var ticksCount = Math.Max(Ticks ?? 3, 2);
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
        // independently round both ends so every actual value stays inside the visible range
        var niceInterval = CalculateNiceInterval(exactMax - exactMin, ticksCount);
        var graphMin = Math.Floor(exactMin / niceInterval) * niceInterval;
        var graphMax = Math.Ceiling(exactMax / niceInterval) * niceInterval;
        var valueRange = graphMax - graphMin;

        // calculate time range for the x-axis
        long timeMin, timeMax;

        if (RangeStart.HasValue && RangeEnd.HasValue && RangeEnd.Value > RangeStart.Value)
        {
            // use the explicitly selected interval
            timeMin = RangeStart.Value.UtcDateTime.Ticks;
            timeMax = RangeEnd.Value.UtcDateTime.Ticks;
        }
        else if (hasDataPoints)
        {
            // "all" view has no fixed interval, so use the actual data boundaries
            timeMin = dataPoints.First().Timestamp!.Value.UtcDateTime.Ticks;
            timeMax = dataPoints.Last().Timestamp!.Value.UtcDateTime.Ticks;
        }
        else
        {
            // empty "all" view fallback
            var now = DateTimeOffset.Now;

            timeMin = now.AddDays(-1).UtcDateTime.Ticks;
            timeMax = now.UtcDateTime.Ticks;
        }

        var timeRange = timeMax - timeMin;

        // only an unbounded view such as "all" can end up with a single instant
        // create a fictional time window (+/- 12 hours)
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

        // the configured tick count is an approximate target;
        // rounding the axis boundaries may require one additional tick
        var actualTicksCount = Math.Max(2, (int)Math.Round((graphMax - graphMin) / niceInterval) + 1);

        for (var i = 0; i < actualTicksCount; i++)
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

        if (graphWidth <= 0) return;

        var graphArea = new Rect(axisLineX, padding, graphWidth, graphHeight);

        // rendering order (z-index): back to front

        // 1. draw the data series first so everything else sits on top of it
        if (hasDataPoints)
        {
            DrawDataSeries(context, graphArea, dataPoints, graphMin, valueRange, timeMin, timeRange, yTickPadding,
                drawableHeight);
        }
        else
        {
            // clear the hit-test points if there are no entries
            _dataPoints.Clear();
        }

        // 2. draw grid, axes and target lines over the data
        DrawGridAndXAxis(context, graphArea, axisPen, timeMin, timeRange);
        DrawTargetLine(context, graphArea, axisPen, graphMin, valueRange, yTickPadding, drawableHeight);
        DrawYAxis(context, graphArea, axisPen, tickTexts, graphMin, valueRange, yTickPadding, drawableHeight,
            textRightMargin, tickEnd, axisLineX);

        // 3. draw tooltip absolutely last to always stay on top
        if (hasDataPoints)
        {
            DrawTooltip(context);
        }
    }

    #endregion
}