// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace easpace.Desktop.Behaviors;

/// <summary>
/// Provides an attached behavior for the ItemsControl that lets the user reorder its items by dragging them by a
/// handle. The handle is any element of an item with the <c>drag-handle</c> class, so the other inputs of the item
/// keep working. While dragging, the dragged item container has the <c>dragging</c> class and the container at the
/// drop position has the <c>drop-before</c> or <c>drop-after</c> class. Escape cancels the drag.
/// </summary>
/// <remarks>
/// The drag captures the pointer instead of starting a system drag and drop, as the items never leave the control.
/// </remarks>
internal class ItemsReorderBehavior : AvaloniaObject
{
    private const string DragHandleClass = "drag-handle";
    private const string DraggingClass = "dragging";
    private const string DropBeforeClass = "drop-before";
    private const string DropAfterClass = "drop-after";

    /// <summary>
    /// Identifies the MoveCommand attached property. Setting a command enables the behavior; the command receives an
    /// <see cref="ItemMoveRequest"/> when an item is dropped at a new position.
    /// </summary>
    public static readonly AttachedProperty<ICommand?> MoveCommandProperty =
        AvaloniaProperty.RegisterAttached<ItemsReorderBehavior, ItemsControl, ICommand?>("MoveCommand");

    // the drag in progress on an items control, if any
    private static readonly AttachedProperty<DragState?> DragStateProperty =
        AvaloniaProperty.RegisterAttached<ItemsReorderBehavior, ItemsControl, DragState?>("DragState");

    /// <summary>
    /// Gets the value of the MoveCommand property.
    /// </summary>
    public static ICommand? GetMoveCommand(ItemsControl element) => element.GetValue(MoveCommandProperty);

    /// <summary>
    /// Sets the value of the MoveCommand property.
    /// </summary>
    public static void SetMoveCommand(ItemsControl element, ICommand? value) => element.SetValue(MoveCommandProperty, value);

    /// <summary>
    /// Initializes static members of the <see cref="ItemsReorderBehavior"/> class.
    /// </summary>
    static ItemsReorderBehavior()
    {
        MoveCommandProperty.Changed.AddClassHandler<ItemsControl>((itemsControl, e) =>
        {
            if (e.OldValue is null && e.NewValue is not null)
            {
                // tunneling, so no input inside the item can swallow the press on the handle first
                itemsControl.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
                itemsControl.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved);
                itemsControl.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
                itemsControl.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
            }
            else if (e.NewValue is null)
            {
                EndDrag(itemsControl, commit: false);

                itemsControl.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
                itemsControl.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
                itemsControl.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
                itemsControl.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
            }
        });
    }

    /// <summary>
    /// Starts a drag when the left button is pressed on the handle of an item.
    /// </summary>
    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ItemsControl itemsControl || itemsControl.GetValue(DragStateProperty) is not null) return;
        if (!e.GetCurrentPoint(itemsControl).Properties.IsLeftButtonPressed) return;
        if (e.Source is not Visual source || !IsOnDragHandle(source, itemsControl)) return;
        if (FindContainer(source, itemsControl) is not { } container) return;

        var state = new DragState(e.Pointer, itemsControl.IndexFromContainer(container), container, TopLevel.GetTopLevel(itemsControl));
        state.KeyDownHandler = (_, args) =>
        {
            if (args.Key != Key.Escape) return;

            args.Handled = true;
            EndDrag(itemsControl, commit: false);
        };

        itemsControl.SetValue(DragStateProperty, state);
        container.Classes.Add(DraggingClass);
        state.TopLevel?.AddHandler(InputElement.KeyDownEvent, state.KeyDownHandler, RoutingStrategies.Tunnel);

        e.Pointer.Capture(itemsControl);
        e.Handled = true;
    }

    /// <summary>
    /// Moves the drop indicator to the gap nearest to the pointer.
    /// </summary>
    private static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not ItemsControl itemsControl || itemsControl.GetValue(DragStateProperty) is not { } state) return;

        var pointerY = e.GetPosition(itemsControl).Y;
        Control? target = null;
        var insertAfter = false;

        // the first item whose middle is below the pointer gets the item before it; past all of them, after the last
        for (var i = 0; i < itemsControl.ItemCount; i++)
        {
            if (itemsControl.ContainerFromIndex(i) is not { } container) continue;
            if (container.TranslatePoint(default, itemsControl) is not { } topLeft) continue;

            target = container;
            insertAfter = pointerY >= topLeft.Y + container.Bounds.Height / 2;

            if (!insertAfter) break;
        }

        ClearDropTarget(state);
        e.Handled = true;

        if (target is null) return;

        var insertIndex = itemsControl.IndexFromContainer(target) + (insertAfter ? 1 : 0);

        // dropping next to itself doesn't move the item, so no gap is marked
        if (insertIndex == state.FromIndex || insertIndex == state.FromIndex + 1) return;

        state.Target = target;
        state.InsertIndex = insertIndex;
        target.Classes.Add(insertAfter ? DropAfterClass : DropBeforeClass);
    }

    private static void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not ItemsControl itemsControl || itemsControl.GetValue(DragStateProperty) is null) return;

        e.Handled = true;
        EndDrag(itemsControl, commit: true);
    }

    // e.g. when the window loses focus in the middle of a drag; a child losing the capture to the items control
    // when the drag starts raises the same bubbling event, which is ignored
    private static void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (sender is ItemsControl itemsControl && ReferenceEquals(e.Source, itemsControl)) EndDrag(itemsControl, commit: false);
    }

    /// <summary>
    /// Ends the drag in progress and removes its marks. When committed, the move command is executed for the marked gap.
    /// </summary>
    private static void EndDrag(ItemsControl itemsControl, bool commit)
    {
        if (itemsControl.GetValue(DragStateProperty) is not { } state) return;

        // cleared first, so the capture lost event raised below finds no drag
        itemsControl.SetValue(DragStateProperty, null);

        var insertIndex = state.InsertIndex;
        state.Source.Classes.Remove(DraggingClass);
        ClearDropTarget(state);

        if (state.KeyDownHandler is not null) state.TopLevel?.RemoveHandler(InputElement.KeyDownEvent, state.KeyDownHandler);
        if (ReferenceEquals(state.Pointer.Captured, itemsControl)) state.Pointer.Capture(null);

        if (!commit || insertIndex is null) return;

        // the item leaves its old place first, which shifts the gaps after it one position up
        var request = new ItemMoveRequest(state.FromIndex, insertIndex > state.FromIndex ? insertIndex.Value - 1 : insertIndex.Value);
        var command = GetMoveCommand(itemsControl);

        if (command?.CanExecute(request) == true) command.Execute(request);
    }

    private static void ClearDropTarget(DragState state)
    {
        state.Target?.Classes.Remove(DropBeforeClass);
        state.Target?.Classes.Remove(DropAfterClass);
        state.Target = null;
        state.InsertIndex = null;
    }

    private static bool IsOnDragHandle(Visual source, ItemsControl itemsControl)
    {
        for (var visual = source; visual is not null && !ReferenceEquals(visual, itemsControl); visual = visual.GetVisualParent())
        {
            if (visual is StyledElement element && element.Classes.Contains(DragHandleClass)) return true;
        }

        return false;
    }

    private static Control? FindContainer(Visual source, ItemsControl itemsControl)
    {
        for (var visual = source; visual is not null && !ReferenceEquals(visual, itemsControl); visual = visual.GetVisualParent())
        {
            if (visual is Control control && itemsControl.IndexFromContainer(control) >= 0) return control;
        }

        return null;
    }

    private sealed class DragState(IPointer pointer, int fromIndex, Control source, TopLevel? topLevel)
    {
        public IPointer Pointer { get; } = pointer;
        public int FromIndex { get; } = fromIndex;
        public Control Source { get; } = source;
        public TopLevel? TopLevel { get; } = topLevel;
        public EventHandler<KeyEventArgs>? KeyDownHandler { get; set; }

        // the container marked as the drop position, and the index the item would be inserted at before it's removed
        public Control? Target { get; set; }
        public int? InsertIndex { get; set; }
    }
}

/// <summary>
/// Represents a request to move an item of a list to another position.
/// </summary>
/// <param name="FromIndex">The current index of the item.</param>
/// <param name="ToIndex">The index of the item after the move.</param>
internal readonly record struct ItemMoveRequest(int FromIndex, int ToIndex);
