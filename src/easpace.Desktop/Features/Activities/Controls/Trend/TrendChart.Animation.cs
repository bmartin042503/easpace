// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using easpace.Desktop.Features.Activities.Constants;

namespace easpace.Desktop.Features.Activities.Controls.Trend;

internal partial class TrendChart
{
    #region Animation

    private void StartDataAnimation(int dataPointCount)
    {
        _animationVersion++;
        _animationStartTime = null;

        // no line exists to animate when fewer than two points are available
        if (dataPointCount <= 1)
        {
            _animationProgress = 1;
            InvalidateVisual();
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);

        // skip animation while the control is not attached to a top-level visual
        if (topLevel is null)
        {
            _animationProgress = 1;
            InvalidateVisual();
            return;
        }

        _animationProgress = 0;

        var animationVersion = _animationVersion;
        topLevel.RequestAnimationFrame(timestamp => OnAnimationFrame(timestamp, animationVersion));

        InvalidateVisual();
    }

    private void OnAnimationFrame(TimeSpan timestamp, int animationVersion)
    {
        // ignore callbacks belonging to an animation that has already been replaced
        if (animationVersion != _animationVersion) return;

        _animationStartTime ??= timestamp;

        var elapsed = timestamp - _animationStartTime.Value;
        var progress = Math.Clamp(elapsed.TotalMilliseconds / DataAnimationDuration.TotalMilliseconds, 0, 1);

        if (progress >= 1)
        {
            _animationProgress = 1;
            InvalidateVisual();
            return;
        }

        _animationProgress = EaseOutCubic(progress);
        InvalidateVisual();

        var topLevel = TopLevel.GetTopLevel(this);

        // stop scheduling frames if the control has been detached
        if (topLevel is null)
        {
            _animationProgress = 1;
            InvalidateVisual();
            return;
        }

        topLevel.RequestAnimationFrame(nextTimestamp => OnAnimationFrame(nextTimestamp, animationVersion));
    }

    private static double EaseOutCubic(double progress)
    {
        return 1 - Math.Pow(1 - progress, 3);
    }

    #endregion

    #region Helpers

    private string FormatXAxisLabel(DateTimeOffset timestamp, TimeSpan visibleTimeRange)
    {
        var culture = CultureInfo.CurrentUICulture;

        return TimeRange switch
        {
            // a day view is primarily about the time of day
            ChartTimeRange.Day => timestamp.ToString("HH:mm", culture),

            // shorter ranges only need the month and day
            ChartTimeRange.Week => timestamp.ToString("MM. dd.", culture),

            ChartTimeRange.Month => timestamp.ToString("MM. dd.", culture),

            // a year view is easier to scan by month
            ChartTimeRange.Year => timestamp.ToString("yyyy. MM.", culture),

            // the year must always be visible in the all view,
            // otherwise dates from different years would be ambiguous
            ChartTimeRange.All when visibleTimeRange.TotalDays > 366 => timestamp.ToString("yyyy. MM.", culture),

            _ => timestamp.ToString("yyyy. MM. dd.", culture)
        };
    }

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

    private static double GetDistance(Point first, Point second)
    {
        return Math.Sqrt(GetSquaredDistance(first, second));
    }

    private static double GetSquaredDistance(Point first, Point second)
    {
        var deltaX = first.X - second.X;
        var deltaY = first.Y - second.Y;

        return deltaX * deltaX + deltaY * deltaY;
    }

    private static Point Interpolate(Point start, Point end, double ratio)
    {
        return new Point(
            start.X + (end.X - start.X) * ratio,
            start.Y + (end.Y - start.Y) * ratio);
    }

    /// <summary>
    /// Returns the currently visible part of a polyline based on the animation progress.
    /// </summary>
    private static (List<Point> Points, int VisibleDataPointCount) GetVisibleSeries(
        List<Point> canvasPoints,
        double progress)
    {
        if (canvasPoints.Count == 0)
        {
            return ([], 0);
        }

        if (canvasPoints.Count == 1 || progress >= 1)
        {
            return (canvasPoints, canvasPoints.Count);
        }

        var totalLength = 0.0;

        for (var i = 1; i < canvasPoints.Count; i++)
        {
            totalLength += GetDistance(canvasPoints[i - 1], canvasPoints[i]);
        }

        if (totalLength <= 0)
        {
            return (canvasPoints, canvasPoints.Count);
        }

        var visibleLength = totalLength * Math.Clamp(progress, 0, 1);
        var drawnLength = 0.0;
        var visibleDataPointCount = 1;

        var visiblePoints = new List<Point> { canvasPoints[0] };

        for (var i = 1; i < canvasPoints.Count; i++)
        {
            var start = canvasPoints[i - 1];
            var end = canvasPoints[i];
            var segmentLength = GetDistance(start, end);

            if (segmentLength <= 0)
            {
                visiblePoints.Add(end);
                visibleDataPointCount++;
                continue;
            }

            if (drawnLength + segmentLength <= visibleLength)
            {
                visiblePoints.Add(end);
                visibleDataPointCount++;
                drawnLength += segmentLength;
                continue;
            }

            var remainingLength = visibleLength - drawnLength;

            if (remainingLength > 0)
            {
                var ratio = remainingLength / segmentLength;
                visiblePoints.Add(Interpolate(start, end, ratio));
            }

            break;
        }

        return (visiblePoints, visibleDataPointCount);
    }

    #endregion
}