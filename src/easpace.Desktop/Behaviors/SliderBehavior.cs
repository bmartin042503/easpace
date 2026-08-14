// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace easpace.Desktop.Behaviors;

/// <summary>
/// Provides an attached behavior for the Slider control that allows the thumb to jump directly
/// to the clicked position on the track. It also enables continuous dragging if the pointer
/// is held down after the jump.
/// </summary>
public class SliderBehavior : AvaloniaObject
{
    /// <summary>
    /// Identifies the JumpToClick attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> JumpToClickProperty =
        AvaloniaProperty.RegisterAttached<SliderBehavior, bool>("JumpToClick", typeof(SliderBehavior));

    /// <summary>
    /// Gets the value of the JumpToClick property.
    /// </summary>
    /// <param name="element">The slider element from which to read the property value.</param>
    /// <returns>True if the jump-to-click behavior is enabled; otherwise, false.</returns>
    public static bool GetJumpToClick(Slider element) => element.GetValue(JumpToClickProperty);

    /// <summary>
    /// Sets the value of the JumpToClick property.
    /// </summary>
    /// <param name="element">The slider element to which the property value is written.</param>
    /// <param name="value">The boolean value to set.</param>
    public static void SetJumpToClick(Slider element, bool value) => element.SetValue(JumpToClickProperty, value);

    /// <summary>
    /// Initializes static members of the <see cref="SliderBehavior"/> class.
    /// </summary>
    static SliderBehavior()
    {
        JumpToClickProperty.Changed.AddClassHandler<Slider>((slider, e) =>
        {
            // add or remove the pointer pressed handler based on the property value
            if (e.NewValue is true)
            {
                slider.AddHandler(InputElement.PointerPressedEvent, OnSliderPointerPressed, RoutingStrategies.Bubble);
            }
            else
            {
                slider.RemoveHandler(InputElement.PointerPressedEvent, OnSliderPointerPressed);
            }
        });
    }

    /// <summary>
    /// Handles the pointer pressed event on the slider. Calculates the jump position
    /// and initiates manual drag if the track is clicked.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a Slider.</param>
    /// <param name="e">The event arguments containing pointer data.</param>
    private static void OnSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Slider slider) return;

        // find the track part in the slider's visual tree
        var track = slider.GetTemplateDescendants().OfType<Track>().FirstOrDefault(x => x.Name == "PART_Track");
        if (track?.Thumb == null) return;

        // check if the click target is the thumb or a child of the thumb
        var isThumbClick = e.Source is Visual sourceVisual &&
                            (ReferenceEquals(sourceVisual, track.Thumb) ||
                             sourceVisual.GetVisualAncestors().Any(a => ReferenceEquals(a, track.Thumb)));

        // if the user clicked the thumb, let the default behavior handle it natively
        if (isThumbClick) return;

        // update the slider value based on the initial click position
        UpdateSliderValue(slider, track, e.GetPosition(track));

        // capture the pointer to the slider to handle continuous dragging smoothly
        e.Pointer.Capture(slider);

        // attach temporary handlers for pointer move and release
        slider.AddHandler(InputElement.PointerMovedEvent, OnSliderPointerMoved, RoutingStrategies.Bubble);
        slider.AddHandler(InputElement.PointerReleasedEvent, OnSliderPointerReleased, RoutingStrategies.Bubble);

        // mark the event as handled to prevent default behavior
        e.Handled = true;
    }

    /// <summary>
    /// Handles the pointer moved event during a manual drag operation.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a Slider.</param>
    /// <param name="e">The event arguments containing pointer data.</param>
    private static void OnSliderPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Slider slider) return;

        // locate the track to calculate relative position
        var track = slider.GetTemplateDescendants().OfType<Track>().FirstOrDefault(x => x.Name == "PART_Track");
        if (track?.Thumb == null) return;

        // update the value as the user moves the pointer
        UpdateSliderValue(slider, track, e.GetPosition(track));
    }

    /// <summary>
    /// Handles the pointer released event, stopping the manual drag operation.
    /// </summary>
    /// <param name="sender">The source of the event, expected to be a Slider.</param>
    /// <param name="e">The event arguments.</param>
    private static void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Slider slider) return;

        // release pointer capture
        e.Pointer.Capture(null);

        // remove the temporary drag handlers
        slider.RemoveHandler(InputElement.PointerMovedEvent, OnSliderPointerMoved);
        slider.RemoveHandler(InputElement.PointerReleasedEvent, OnSliderPointerReleased);
    }

    /// <summary>
    /// Calculates and applies the new slider value based on the pointer position relative to the track.
    /// </summary>
    /// <param name="slider">The slider instance to update.</param>
    /// <param name="track">The track representing the draggable area.</param>
    /// <param name="point">The pointer coordinates relative to the track.</param>
    private static void UpdateSliderValue(Slider slider, Track track, Point point)
    {
        if (track.Thumb == null) return;
     
        // calculate the valid draggable length of the track
        var trackLength = slider.Orientation == Avalonia.Layout.Orientation.Horizontal
            ? track.Bounds.Width - track.Thumb.Bounds.Width
            : track.Bounds.Height - track.Thumb.Bounds.Height;

        if (trackLength <= 0) return;

        // calculate the relative click position offset by half the thumb size
        var clickPosition = slider.Orientation == Avalonia.Layout.Orientation.Horizontal
            ? point.X - track.Thumb.Bounds.Width / 2
            : point.Y - track.Thumb.Bounds.Height / 2;

        // determine the percentage of the position along the track
        var percent = Math.Clamp(clickPosition / trackLength, 0.0, 1.0);

        // adjust the percentage for orientation and inverted direction
        if (slider is { Orientation: Avalonia.Layout.Orientation.Vertical, IsDirectionReversed: false } or
            { Orientation: Avalonia.Layout.Orientation.Horizontal, IsDirectionReversed: true })
            percent = 1 - percent;

        // apply the calculated value to the slider
        var range = slider.Maximum - slider.Minimum;
        var newValue = slider.Minimum + range * percent;

        slider.Value = Math.Clamp(newValue, slider.Minimum, slider.Maximum);
    }
}