// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace easpace.Desktop.Features.Activities.Controls;

internal class MilestoneChart : Control
{
    #region Fields

    private readonly FontFamily _fontFamily = new("avares://easpace.Desktop/Assets/Fonts#Poppins");
    
    // fields for animation logic
    private double _animatedValue;
    private double _animStartValue;
    private double _animTargetValue ;
    private DateTime _animStartTime;
    private DispatcherTimer? _animTimer;
    private readonly TimeSpan _animDuration = TimeSpan.FromMilliseconds(800); // duration of the fill animation

    #endregion

    #region Styled & Direct Properties

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MilestoneChart, double>(nameof(Value));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MilestoneChart, double>(nameof(Maximum), 100.0);

    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<MilestoneChart, string?>(nameof(Unit));

    public static readonly StyledProperty<double> RingThicknessProperty =
        AvaloniaProperty.Register<MilestoneChart, double>(nameof(RingThickness), 20.0);

    public static readonly StyledProperty<IBrush?> ProgressBrushProperty =
        AvaloniaProperty.Register<MilestoneChart, IBrush?>(nameof(ProgressBrush));
    
    public static readonly StyledProperty<IBrush?> UnitBrushProperty =
        AvaloniaProperty.Register<MilestoneChart, IBrush?>(nameof(UnitBrush));

    public static readonly StyledProperty<IBrush?> TrackBrushProperty =
        AvaloniaProperty.Register<MilestoneChart, IBrush?>(nameof(TrackBrush));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<MilestoneChart, double>(nameof(FontSize), 32.0);

    public static readonly StyledProperty<double> UnitFontSizeProperty =
        AvaloniaProperty.Register<MilestoneChart, double>(nameof(UnitFontSize), 16.0);

    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<MilestoneChart, Thickness>(nameof(Padding));

    #endregion

    #region Properties

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public double RingThickness
    {
        get => GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    public IBrush? ProgressBrush
    {
        get => GetValue(ProgressBrushProperty);
        set => SetValue(ProgressBrushProperty, value);
    }
    
    public IBrush? UnitBrush
    {
        get => GetValue(UnitBrushProperty);
        set => SetValue(UnitBrushProperty, value);
    }

    public IBrush? TrackBrush
    {
        get => GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double UnitFontSize
    {
        get => GetValue(UnitFontSizeProperty);
        set => SetValue(UnitFontSizeProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    #endregion

    #region Initialization

    static MilestoneChart()
    {
        AffectsRender<MilestoneChart>(
            ValueProperty,
            MaximumProperty,
            UnitProperty,
            RingThicknessProperty,
            ProgressBrushProperty,
            UnitBrushProperty,
            TrackBrushProperty,
            FontSizeProperty,
            UnitFontSizeProperty,
            PaddingProperty
        );
    }

    #endregion

    #region Lifecycle & Animation

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // start animation from 0 to current value when control loads
        StartAnimation(0, Value);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        // prevent memory leaks by stopping the timer when control is removed
        _animTimer?.Stop();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        // if value changes dynamically later, animate from current visual value to new value
        if (change.Property == ValueProperty)
        {
            var newVal = change.NewValue is double nv ? nv : 0;
            StartAnimation(_animatedValue, newVal);
        }
    }

    private void StartAnimation(double from, double to)
    {
        _animStartTime = DateTime.Now;
        _animStartValue = from;
        _animTargetValue = to;

        if (_animTimer == null)
        {
            _animTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // roughly 60fps
            };
            
            _animTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - _animStartTime;
                var progress = elapsed.TotalMilliseconds / _animDuration.TotalMilliseconds;

                if (progress >= 1.0)
                {
                    // animation finished
                    _animatedValue = _animTargetValue;
                    _animTimer.Stop();
                }
                else
                {
                    // applying cubic ease-out function for smooth deceleration at the end
                    var easedProgress = 1 - Math.Pow(1 - progress, 3);
                    _animatedValue = _animStartValue + (_animTargetValue - _animStartValue) * easedProgress;
                }
                
                // force the control to redraw with the new _animatedValue
                InvalidateVisual();
            };
        }
        
        _animTimer.Start();
    }

    #endregion

    #region Rendering

    public override void Render(DrawingContext context)
    {
        // apply padding to the available drawing bounds
        var padding = Padding;
        var width = Math.Max(0, Bounds.Width - padding.Left - padding.Right);
        var height = Math.Max(0, Bounds.Height - padding.Top - padding.Bottom);

        // find the smallest dimension to ensure a perfect circle
        var size = Math.Min(width, height);

        if (size <= 0) return;

        // determine the exact center of the control, offset by top/left padding
        var center = new Point(padding.Left + width / 2, padding.Top + height / 2);

        // calculate radius, subtracting half the thickness so the stroke doesn't clip outside
        var radius = size / 2 - RingThickness / 2;
        if (radius <= 0) return;

        // define the brushes for the background track and the progress fill
        var trackPen = new Pen(TrackBrush, RingThickness);
        var progressPen = new Pen(ProgressBrush, RingThickness, null, PenLineCap.Round, PenLineJoin.Round);

        // draw the full background ring first
        context.DrawEllipse(null, trackPen, center, radius, radius);

        // calculate a safe progress ratio between 0.0 and 1.0 using the animated value
        var max = Math.Max(Maximum, 1);
        var val = Math.Clamp(_animatedValue, 0, max);
        var progressPercentage = val / max;

        if (progressPercentage > 0)
        {
            if (progressPercentage >= 1.0)
            {
                // if 100% or more, just draw a solid circle to save performance
                context.DrawEllipse(null, progressPen, center, radius, radius);
            }
            else
            {
                // convert the percentage into a radian angle (full circle = 2 * PI)
                var angle = progressPercentage * 2 * Math.PI;

                // start at 12 o'clock (-90 degrees in radians) instead of the default 3 o'clock
                const double startAngle = -Math.PI / 2;
                var endAngle = startAngle + angle;

                // use cosine (x) and sine (y) to translate the angle into exact starting pixels
                var startPoint = new Point(
                    center.X + radius * Math.Cos(startAngle),
                    center.Y + radius * Math.Sin(startAngle));

                // find the exact ending pixel based on the calculated progress angle
                var endPoint = new Point(
                    center.X + radius * Math.Cos(endAngle),
                    center.Y + radius * Math.Sin(endAngle));

                // if progress > 50%, force the renderer to take the long path around the circle
                var isLargeArc = angle > Math.PI;

                var geometry = new StreamGeometry();
                using (var geoContext = geometry.Open())
                {
                    geoContext.BeginFigure(startPoint, isFilled: false);
                    geoContext.ArcTo(
                        endPoint,
                        new Size(radius, radius),
                        rotationAngle: 0,
                        isLargeArc,
                        SweepDirection.Clockwise);
                }

                // draw the calculated arc onto the canvas
                context.DrawGeometry(null, progressPen, geometry);
            }
        }

        // format main text. using "0" format so it doesn't show crazy decimals during animation
        var mainTextString = $"{_animatedValue:0}/{Maximum}";
        
        var mainTextLayout = new TextLayout(
            mainTextString,
            new Typeface(_fontFamily),
            FontSize,
            progressPen.Brush,
            TextAlignment.Center);

        TextLayout? unitTextLayout = null;
        
        // setup unit text layout if a unit was provided
        if (!string.IsNullOrWhiteSpace(Unit))
        {
            unitTextLayout = new TextLayout(
                Unit,
                new Typeface(_fontFamily),
                UnitFontSize,
                UnitBrush ?? progressPen.Brush,
                TextAlignment.Center);
        }

        // calculate total bounding box for both texts to ensure centering
        var totalHeight = mainTextLayout.Height + (unitTextLayout?.Height ?? 0);
        var maxWidth = Math.Max(mainTextLayout.Width, unitTextLayout?.Width ?? 0);

        // calculate the exact inner radius of the donut hole (outer radius minus half the ring thickness)
        var innerRadius = radius - RingThickness / 2;

        // calculate the distance from the center to the corner of the text bounding box using pythagorean theorem
        var textHalfWidth = maxWidth / 2;
        var textHalfHeight = totalHeight / 2;
        var textCornerDistance = Math.Sqrt(textHalfWidth * textHalfWidth + textHalfHeight * textHalfHeight);

        // only draw the texts if their corners fit completely inside the inner empty circle
        if (textCornerDistance <= innerRadius)
        {
            // starting Y position so the whole text block is centered vertically
            var startY = center.Y - textHalfHeight;

            // draw main value/max text
            var mainTextPos = new Point(center.X - mainTextLayout.Width / 2, startY);
            mainTextLayout.Draw(context, mainTextPos);

            // draw unit text below main text if it exists
            if (unitTextLayout != null)
            {
                var unitTextPos = new Point(center.X - unitTextLayout.Width / 2, startY + mainTextLayout.Height);
                unitTextLayout.Draw(context, unitTextPos);
            }
        }
    }

    #endregion
}