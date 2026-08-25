using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace easpace.Desktop.Controls;

/// <summary>
/// An animated liquid gradient background control that uses moving blurred ellipses.
/// </summary>
internal class BlobsGradientBackground : Control
{
    private readonly DispatcherTimer _timer;
    private readonly Random _rnd = new();
    private readonly List<Blob> _blobs = new();
    
    private readonly Border _bgBorder;
    private readonly BlobCanvas _blobsCanvas;

    public static readonly StyledProperty<IBrush> FromBrushProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, IBrush>(nameof(FromBrush));

    public static readonly StyledProperty<IBrush> ToBrushProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, IBrush>(nameof(ToBrush));
    
    public static readonly StyledProperty<IBrush> BackgroundBrushProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, IBrush>(nameof(BackgroundBrush), new SolidColorBrush(Colors.Transparent));
    
    public static readonly StyledProperty<int> EllipseCountProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, int>(nameof(EllipseCount), 6);
    
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<BlobsGradientBackground, double>(nameof(Speed), 1.0);

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
        // the main control strictly clips anything that bleeds outside
        ClipToBounds = true;
        
        // background layer without blur
        _bgBorder = new Border();
        
        // blobs layer with heavy blur
        _blobsCanvas = new BlobCanvas(this)
        {
            Effect = new BlurEffect { Radius = 140 }
        };

        // add child layers to the visual tree
        VisualChildren.Add(_bgBorder);
        VisualChildren.Add(_blobsCanvas);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnTick;
    }

    // allocate space for the internal layers
    protected override Size MeasureOverride(Size availableSize)
    {
        _bgBorder.Measure(availableSize);
        
        // inflate the blob canvas by 400px (200px each side) to hide the faded blur edges
        var expandedWidth = double.IsInfinity(availableSize.Width) ? availableSize.Width : availableSize.Width + 400;
        var expandedHeight = double.IsInfinity(availableSize.Height) ? availableSize.Height : availableSize.Height + 400;
        
        _blobsCanvas.Measure(new Size(expandedWidth, expandedHeight));
        
        return base.MeasureOverride(availableSize);
    }

    // position the internal layers
    protected override Size ArrangeOverride(Size finalSize)
    {
        _bgBorder.Arrange(new Rect(finalSize));
        
        // offset the blob canvas by -200px so it centers over the control but bleeds out on all sides
        _blobsCanvas.Arrange(new Rect(-200, -200, finalSize.Width + 400, finalSize.Height + 400));
        
        return finalSize;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        // ensure background is applied on load
        _bgBorder.Background = BackgroundBrush; 
        
        InitializeBlobs();
        
        if (IsVisible)
        {
            _timer.Start();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer.Stop();
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
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }
        
        if (change.Property == FromBrushProperty || 
            change.Property == ToBrushProperty || 
            change.Property == EllipseCountProperty)
        {
            InitializeBlobs();
        }
        
        // forward background changes to the crisp border layer
        if (change.Property == BackgroundBrushProperty)
        {
            _bgBorder.Background = BackgroundBrush;
        }
    }

    private void InitializeBlobs()
    {
        _blobs.Clear();
        // we use the oversized canvas bounds for spawn area
        var bounds = _blobsCanvas.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var count = Math.Max(1, Math.Min(10, EllipseCount));

        for (var i = 0; i < count; i++)
        {
            var transition = _rnd.NextDouble();
            var interpolatedColor = InterpolateColor(FromBrush, ToBrush, transition);
            
            var finalColor = Color.FromArgb(
                (byte)_rnd.Next(160, 255),
                interpolatedColor.R,
                interpolatedColor.G,
                interpolatedColor.B
            );

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
    
    private Color InterpolateColor(IBrush fromBrush, IBrush toBrush, double transition)
    {
        var fromColor = (fromBrush as ISolidColorBrush)?.Color ?? Colors.Transparent;
        var toColor = (toBrush as ISolidColorBrush)?.Color ?? Colors.Transparent;
        
        return Color.FromArgb(
            (byte)(fromColor.A + (toColor.A - fromColor.A) * transition),
            (byte)(fromColor.R + (toColor.R - fromColor.R) * transition),
            (byte)(fromColor.G + (toColor.G - fromColor.G) * transition),
            (byte)(fromColor.B + (toColor.B - fromColor.B) * transition)
        );
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var bounds = _blobsCanvas.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var currentSpeed = Speed; 

        foreach (var blob in _blobs)
        {
            blob.X += blob.Vx * currentSpeed;
            blob.Y += blob.Vy * currentSpeed;

            // bounce off the edges of the oversized canvas bounds
            if (blob.X < 0) blob.Vx = Math.Abs(blob.Vx);
            if (blob.X > bounds.Width) blob.Vx = -Math.Abs(blob.Vx);
            
            if (blob.Y < 0) blob.Vy = Math.Abs(blob.Vy);
            if (blob.Y > bounds.Height) blob.Vy = -Math.Abs(blob.Vy);
        }

        // redraw only the canvas layer
        _blobsCanvas.InvalidateVisual();
    }

    /// <summary>
    /// Internal drawing surface solely for rendering the blurred blobs.
    /// </summary>
    private class BlobCanvas : Control
    {
        private readonly BlobsGradientBackground _parent;

        public BlobCanvas(BlobsGradientBackground parent)
        {
            _parent = parent;
        }

        public override void Render(DrawingContext context)
        {
            // background drawing is omitted here because it's handled by _bgBorder
            foreach (var blob in _parent._blobs)
            {
                context.DrawEllipse(blob.Brush, null, new Point(blob.X, blob.Y), blob.Radius, blob.Radius);
            }
        }
    }

    private class Blob
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public IBrush Brush { get; set; } = Brushes.Transparent;
    }
}