// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;

namespace easpace.Desktop.Features.Wellness.Controls;

/// <summary>
/// Shows the breathing circle: a fixed inner circle, an outer circle that grows and shrinks with the breath, and a
/// text in the center.
/// </summary>
internal partial class BreathingCircle : UserControl
{
    /// <summary>
    /// Identifies the <see cref="CircleSize"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CircleSizeProperty =
        AvaloniaProperty.Register<BreathingCircle, double>(nameof(CircleSize), 96);

    /// <summary>
    /// Identifies the <see cref="Text"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<BreathingCircle, string?>(nameof(Text));

    /// <summary>
    /// Gets or sets the diameter of the outer circle, between 96 (breathed out) and 192 (breathed in).
    /// </summary>
    public double CircleSize
    {
        get => GetValue(CircleSizeProperty);
        set => SetValue(CircleSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the text in the center, such as the seconds left of the step. Changes fade in.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public BreathingCircle()
    {
        InitializeComponent();
    }
}
