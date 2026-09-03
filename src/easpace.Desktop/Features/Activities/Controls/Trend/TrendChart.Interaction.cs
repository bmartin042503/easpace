// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Input;
using System.Linq;

namespace easpace.Desktop.Features.Activities.Controls.Trend;

internal partial class TrendChart
{
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_dataPoints.Count == 0) return;

        var currentPosition = e.GetPosition(this);

        var closestPoint = _dataPoints
            .MinBy(dp => GetSquaredDistance(dp.CanvasPoint, currentPosition));

        var distanceSquared = GetSquaredDistance(
            closestPoint.CanvasPoint,
            currentPosition);

        if (distanceSquared <= TooltipHitRadius * TooltipHitRadius)
        {
            if (_hoveredDataPoint != closestPoint.DataPoint)
            {
                _hoveredDataPoint = closestPoint.DataPoint;
                _hoveredPoint = closestPoint.CanvasPoint;
                InvalidateVisual();
            }

            return;
        }

        if (_hoveredDataPoint != null)
        {
            _hoveredDataPoint = null;
            InvalidateVisual();
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
}