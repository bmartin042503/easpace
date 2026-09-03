// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.Features.Activities.Controls.Trend;

internal partial class TrendChart
{
    private void DrawGridAndXAxis(DrawingContext context, Rect graphArea, Pen axisPen, long timeMin, long timeRange)
    {
        // base x-axis line
        context.DrawLine(
            axisPen,
            new Point(graphArea.Left, graphArea.Bottom),
            new Point(graphArea.Right, graphArea.Bottom));

        // at least two lines are required to calculate the ratios safely
        var gridLines = Math.Max(GridLines ?? 7, 2);

        var visibleTimeRange = new TimeSpan(timeRange);
        var gridPen = new Pen(GridLineBrush) { DashStyle = DashStyle.Dash };

        for (var i = 0; i < gridLines; i++)
        {
            var ratio = (double)i / (gridLines - 1);
            var x = graphArea.Left + ratio * graphArea.Width;

            // draw vertical grid line (skip the first one to avoid overlapping the y-axis)
            if (i > 0)
            {
                context.DrawLine(
                    gridPen,
                    new Point(x, graphArea.Top),
                    new Point(x, graphArea.Bottom));
            }

            var tickTicks = timeMin + (long)(timeRange * ratio);

            if (i == gridLines - 1 && timeRange > 0)
            {
                tickTicks--;
            }

            var tickDate = new DateTimeOffset(tickTicks, TimeSpan.Zero).ToLocalTime();

            var xText = new TextLayout(
                FormatXAxisLabel(tickDate, visibleTimeRange),
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

            var textX = textRightMargin - 6 - tick.Layout.Width;
            tick.Layout.Draw(context, new Point(textX, y - tick.Layout.Height / 2));
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
        if (!Target.HasValue) return;

        var yRatio = (Target.Value - graphMin) / valueRange;
        var targetY = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;

        context.DrawLine(axisPen, new Point(graphArea.Left, targetY), new Point(graphArea.Right, targetY));

        var formattedTarget = Target.Value.ToString("0.##", CultureInfo.CurrentCulture);

        var targetValue = string.IsNullOrWhiteSpace(Unit)
            ? formattedTarget
            : $"{formattedTarget} {Unit}";

        var targetText = new TextLayout(
            $"{LocalizationService.GetString("Activities.NumericActivity.Target")}: {targetValue}",
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
        var canvasPoints = new List<Point>(dataPoints.Count);

        foreach (var dataPoint in dataPoints)
        {
            var timestampTicks = dataPoint.Timestamp!.Value.UtcDateTime.Ticks;

            var xRatio = (double)(timestampTicks - timeMin) / timeRange;
            var x = graphArea.X + xRatio * graphArea.Width;

            var yRatio = (dataPoint.Value - graphMin) / valueRange;
            var y = graphArea.Bottom - yTickPadding - yRatio * drawableHeight;

            canvasPoints.Add(new Point(x, y));
        }

        var visibleSeries = GetVisibleSeries(canvasPoints, _animationProgress);
        var visibleCanvasPoints = visibleSeries.Points;

        _dataPoints.Clear();

        // store only points already reached by the animation for marker rendering and hover hit-testing
        for (var i = 0; i < visibleSeries.VisibleDataPointCount; i++)
        {
            _dataPoints.Add((canvasPoints[i], dataPoints[i]));
        }

        var fillGeometry = new StreamGeometry();
        var strokeGeometry = new StreamGeometry();

        // build the visible geometric path for the gradient area
        using (var fillContext = fillGeometry.Open())
        {
            var firstPoint = visibleCanvasPoints[0];

            fillContext.BeginFigure(new Point(firstPoint.X, graphArea.Bottom));
            fillContext.LineTo(firstPoint);

            for (var i = 1; i < visibleCanvasPoints.Count; i++)
            {
                fillContext.LineTo(visibleCanvasPoints[i]);
            }

            var lastPoint = visibleCanvasPoints[^1];
            fillContext.LineTo(new Point(lastPoint.X, graphArea.Bottom));
        }

        // build the visible part of the animated stroke line
        using (var strokeContext = strokeGeometry.Open())
        {
            strokeContext.BeginFigure(visibleCanvasPoints[0], false);

            for (var i = 1; i < visibleCanvasPoints.Count; i++)
            {
                strokeContext.LineTo(visibleCanvasPoints[i]);
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

        var formattedValue = _hoveredDataPoint.Value.ToString("0.##", CultureInfo.CurrentCulture);

        var tooltipValue = string.IsNullOrWhiteSpace(Unit)
            ? formattedValue
            : $"{formattedValue} {Unit}";

        var valueText = new TextLayout(
            tooltipValue,
            new Typeface(_fontFamily),
            14,
            TooltipValueForeground,
            TextAlignment.Center);

        var tooltipTimestamp = _hoveredDataPoint.Timestamp!.Value.ToLocalTime();

        var tooltipDateText = TimeRange == ChartTimeRange.Day
            ? tooltipTimestamp.ToString(
                "yyyy. MM. dd. HH:mm:ss",
                CultureInfo.CurrentUICulture)
            : tooltipTimestamp.ToString(
                "yyyy. MM. dd.",
                CultureInfo.CurrentUICulture);

        var dateText = new TextLayout(
            tooltipDateText,
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
}