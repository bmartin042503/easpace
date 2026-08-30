// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace easpace.Desktop.Controls;

/// <summary>
/// An animated liquid gradient background control that uses moving blurred ellipses.
/// </summary>
internal class BlobsGradientBackground : Control
{
    private const double ReferenceFrameRate = 60.0;
    private const double MaxDeltaSeconds = 0.05;

    private readonly Random _rnd = new();
    private readonly List<Blob> _blobs = new();

    private readonly Border _bgBorder;
    private readonly BlobCanvas _blobsCanvas;

    private TopLevel? _topLevel;
    private Window? _window;

    private bool _isAttached;
    private bool _animationFramePending;
    private TimeSpan? _previousFrameTime;

    public static readonly StyledProperty<IBrush> FromBrushProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, IBrush>(nameof(FromBrush));

    public static readonly StyledProperty<IBrush> ToBrushProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, IBrush>(nameof(ToBrush));

    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, IBrush>(
            nameof(BackgroundBrush),
            new SolidColorBrush(Colors.Transparent));

    public static readonly StyledProperty<int> EllipseCountProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, int>(
            nameof(EllipseCount),
            6);

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, double>(
            nameof(Speed),
            1.0);

    public IBrush FromBrush
    {
        get => GetValue(FromBrushProperty);
        set => SetValue(FromBrushProperty, value);
    }

    public IBrush ToBrush
    {
        get => GetValue(ToBrushProperty);
        set => SetValue(ToBrushProperty, value);
    }

    public IBrush BackgroundBrush
    {
        get => GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public int EllipseCount
    {
        get => GetValue(EllipseCountProperty);
        set => SetValue(EllipseCountProperty, value);
    }

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public BlobsGradientBackground()
    {
        ClipToBounds = true;

        _bgBorder = new Border();

        _blobsCanvas = new BlobCanvas(this)
        {
            Effect = new BlurEffect
            {
                Radius = 90
            }
        };

        VisualChildren.Add(_bgBorder);
        VisualChildren.Add(_blobsCanvas);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _bgBorder.Measure(availableSize);

        var expandedWidth = double.IsInfinity(availableSize.Width)
            ? availableSize.Width
            : availableSize.Width + 400;

        var expandedHeight = double.IsInfinity(availableSize.Height)
            ? availableSize.Height
            : availableSize.Height + 400;

        _blobsCanvas.Measure(new Size(expandedWidth, expandedHeight));

        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _bgBorder.Arrange(new Rect(finalSize));

        _blobsCanvas.Arrange(new Rect(-200, -200, finalSize.Width + 400, finalSize.Height + 400));

        return finalSize;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _isAttached = true;

        _bgBorder.Background = BackgroundBrush;

        _topLevel = TopLevel.GetTopLevel(this);
        _window = _topLevel as Window;

        if (_window is not null)
        {
            _window.PropertyChanged += OnWindowPropertyChanged;
        }

        InitializeBlobs();

        StartAnimation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        _previousFrameTime = null;

        if (_window is not null)
        {
            _window.PropertyChanged -= OnWindowPropertyChanged;
        }

        _window = null;
        _topLevel = null;

        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (_blobs.Count == 0 || e.PreviousSize.Width == 0)
        {
            InitializeBlobs();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty)
        {
            if (IsVisible)
            {
                StartAnimation();
            }
            else
            {
                _previousFrameTime = null;
            }
        }

        if (change.Property == FromBrushProperty ||
            change.Property == ToBrushProperty ||
            change.Property == EllipseCountProperty)
        {
            InitializeBlobs();
        }

        if (change.Property == BackgroundBrushProperty)
        {
            _bgBorder.Background = BackgroundBrush;
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Window.WindowStateProperty)
        {
            return;
        }

        if (_window?.WindowState == WindowState.Minimized)
        {
            _previousFrameTime = null;
            return;
        }

        StartAnimation();
    }

    private void StartAnimation()
    {
        _previousFrameTime = null;
        RequestNextAnimationFrame();
    }

    private void RequestNextAnimationFrame()
    {
        if (!ShouldAnimate() || _animationFramePending || _topLevel is null)
        {
            return;
        }

        _animationFramePending = true;
        _topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private bool ShouldAnimate()
    {
        if (!_isAttached || !IsVisible)
        {
            return false;
        }

        if (_window is not null && _window.WindowState == WindowState.Minimized)
        {
            return false;
        }

        return true;
    }

    private void OnAnimationFrame(TimeSpan elapsed)
    {
        _animationFramePending = false;

        if (!ShouldAnimate())
        {
            _previousFrameTime = null;
            return;
        }

        if (_previousFrameTime.HasValue)
        {
            var delta = elapsed - _previousFrameTime.Value;

            var deltaSeconds = Math.Clamp(delta.TotalSeconds, 0, MaxDeltaSeconds);

            var frameScale = deltaSeconds * ReferenceFrameRate;

            UpdateBlobs(frameScale);

            _blobsCanvas.InvalidateVisual();
        }

        _previousFrameTime = elapsed;

        RequestNextAnimationFrame();
    }

    private void UpdateBlobs(double frameScale)
    {
        var bounds = _blobsCanvas.Bounds;

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var currentSpeed = Speed;

        foreach (var blob in _blobs)
        {
            blob.X += blob.Vx * currentSpeed * frameScale;
            blob.Y += blob.Vy * currentSpeed * frameScale;

            if (blob.X < 0)
            {
                blob.X = 0;
                blob.Vx = Math.Abs(blob.Vx);
            }
            else if (blob.X > bounds.Width)
            {
                blob.X = bounds.Width;
                blob.Vx = -Math.Abs(blob.Vx);
            }

            if (blob.Y < 0)
            {
                blob.Y = 0;
                blob.Vy = Math.Abs(blob.Vy);
            }
            else if (blob.Y > bounds.Height)
            {
                blob.Y = bounds.Height;
                blob.Vy = -Math.Abs(blob.Vy);
            }
        }
    }

    private void InitializeBlobs()
    {
        _blobs.Clear();

        var bounds = _blobsCanvas.Bounds;

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var count = Math.Clamp(EllipseCount, 1, 10);

        for (var i = 0; i < count; i++)
        {
            var transition = _rnd.NextDouble();

            var interpolatedColor = InterpolateColor(
                FromBrush,
                ToBrush,
                transition);

            var finalColor = Color.FromArgb(
                (byte)_rnd.Next(160, 255),
                interpolatedColor.R,
                interpolatedColor.G,
                interpolatedColor.B);

            _blobs.Add(new Blob
            {
                X = _rnd.NextDouble() * bounds.Width,
                Y = _rnd.NextDouble() * bounds.Height,
                Radius = _rnd.NextDouble() * 150 + 200,
                Vx = (_rnd.NextDouble() - 0.5) * 5,
                Vy = (_rnd.NextDouble() - 0.5) * 5,
                Brush = new SolidColorBrush(finalColor)
            });
        }
    }

    private static Color InterpolateColor(IBrush fromBrush, IBrush toBrush, double transition)
    {
        var fromColor =
            (fromBrush as ISolidColorBrush)?.Color
            ?? Colors.Transparent;

        var toColor =
            (toBrush as ISolidColorBrush)?.Color
            ?? Colors.Transparent;

        return Color.FromArgb(
            (byte)(fromColor.A +
                   (toColor.A - fromColor.A) * transition),
            (byte)(fromColor.R +
                   (toColor.R - fromColor.R) * transition),
            (byte)(fromColor.G +
                   (toColor.G - fromColor.G) * transition),
            (byte)(fromColor.B +
                   (toColor.B - fromColor.B) * transition));
    }

    private sealed class BlobCanvas : Control
    {
        private readonly BlobsGradientBackground _parent;

        public BlobCanvas(BlobsGradientBackground parent)
        {
            _parent = parent;
        }

        public override void Render(DrawingContext context)
        {
            foreach (var blob in _parent._blobs)
            {
                context.DrawEllipse(
                    blob.Brush,
                    null,
                    new Point(blob.X, blob.Y),
                    blob.Radius,
                    blob.Radius);
            }
        }
    }

    private sealed class Blob
    {
        public double X { get; set; }
        public double Y { get; set; }

        public double Radius { get; set; }

        public double Vx { get; set; }
        public double Vy { get; set; }

        public IBrush Brush { get; set; } = Brushes.Transparent;
    }
}