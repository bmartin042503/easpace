// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace easpace.Desktop.Views;

internal partial class MainWindow : Window
{
    private bool _isDragging;
    private Point _dragStartPoint;
    
    public MainWindow()
    {
        InitializeComponent();
        
        DialogHost.PropertyChanged += (_, args) =>
        {
            if (args.Property == ContentProperty && args.NewValue != null)
            {
                if (DialogHost.RenderTransform is TranslateTransform transform)
                {
                    transform.X = 0;
                    transform.Y = 0;
                }
            }
        };
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ExtendClientAreaToDecorationsHint = true;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // removes title bar on macOS, but keep window control buttons in the top left corner
            ExtendClientAreaToDecorationsHint = true;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            
        }
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        var point = e.GetCurrentPoint(this);

        const int dragZoneHeight = 30;

        if (point.Properties.IsLeftButtonPressed && point.Position.Y <= dragZoneHeight)
        {
            BeginMoveDrag(e);
        }
    }
    
    private void DialogHost_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;

        var sourceControl = e.Source as Control;
        var isAllowedToDrag = sourceControl is { Name: "DialogRootBorder" or "DialogRootTitle" };

        if (!isAllowedToDrag) 
        {
            return;
        }
        
        var properties = e.GetCurrentPoint(this).Properties;
        
        if (properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            
            e.Pointer.Capture(control);
        }
    }
    
    private void DialogHost_PointerMoved(object sender, PointerEventArgs e)
    {
        if (!_isDragging) return;
        
        if (sender is not Control { RenderTransform: TranslateTransform transform } control) return;

        var currentPoint = e.GetPosition(this);
        
        var deltaX = currentPoint.X - _dragStartPoint.X;
        var deltaY = currentPoint.Y - _dragStartPoint.Y;
    
        var expectedX = transform.X + deltaX;
        var expectedY = transform.Y + deltaY;
        
        var maxX = Math.Max(0, (Bounds.Width - control.Bounds.Width) / 2);
        var maxY = Math.Max(0, (Bounds.Height - control.Bounds.Height) / 2);
        
        var previousX = transform.X;
        var previousY = transform.Y;
        
        transform.X = Math.Clamp(expectedX, -maxX, maxX);
        transform.Y = Math.Clamp(expectedY, -maxY, maxY);
        
        var actualMovedX = transform.X - previousX;
        var actualMovedY = transform.Y - previousY;

        _dragStartPoint = new Point(_dragStartPoint.X + actualMovedX, _dragStartPoint.Y + actualMovedY);
    }
    
    private void DialogHost_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        e.Pointer.Capture(null);
    }
}
