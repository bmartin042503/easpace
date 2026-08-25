// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace easpace.Desktop.Utils;

/// <summary>
/// Provides attached behaviors to execute commands based on view lifecycle events.
/// </summary>
/// <remarks>
/// Use this class to bind ViewModel commands to the loading event of a View (UserControl or Window).
/// </remarks>
internal static class ViewInitialization
{
    static ViewInitialization()
    {
        CommandProperty.Changed.AddClassHandler<Control, ICommand?>(OnCommandChanged);
    }

    /// <summary>
    /// Defines the attached property for the command to execute when the control is attached to the visual tree.
    /// </summary>
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<Control, ICommand?>(
            "Command",
            typeof(ViewInitialization));

    /// <summary>
    /// Defines the attached property for the command parameter.
    /// </summary>
    public static readonly AttachedProperty<object?> CommandParameterProperty =
        AvaloniaProperty.RegisterAttached<Control, object?>(
            "CommandParameter",
            typeof(ViewInitialization));

    /// <summary>
    /// Handles changes to the Command attached property.
    /// </summary>
    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs<ICommand?> args)
    {
        // if there was a previous value, unsubscribe to prevent memory leaks
        if (args.OldValue.Value is not null)
        {
            control.AttachedToVisualTree -= HandleAttachedToVisualTree;
        }

        // if there is a new value, subscribe to the event
        if (args.NewValue.Value is not null)
        {
            control.AttachedToVisualTree += HandleAttachedToVisualTree;
        }
    }

    /// <summary>
    /// The actual event handler that executes the command.
    /// </summary>
    private static void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not Control control) return;

        var command = GetCommand(control);
        var parameter = GetCommandParameter(control);

        if (command != null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    /// <summary>
    /// Sets the value of the <see cref="CommandProperty"/> on the specified element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value">The command to execute.</param>
    public static void SetCommand(AvaloniaObject element, ICommand? value)
        => element.SetValue(CommandProperty, value);

    /// <summary>
    /// Gets the value of the <see cref="CommandProperty"/> from the specified element.
    /// </summary>
    /// <param name="element">The source element.</param>
    /// <returns>The attached command.</returns>
    public static ICommand? GetCommand(AvaloniaObject element)
        => element.GetValue(CommandProperty);

    /// <summary>
    /// Sets the value of the <see cref="CommandParameterProperty"/> on the specified element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value">The parameter to pass to the command.</param>
    public static void SetCommandParameter(AvaloniaObject element, object? value)
        => element.SetValue(CommandParameterProperty, value);

    /// <summary>
    /// Gets the value of the <see cref="CommandParameterProperty"/> from the specified element.
    /// </summary>
    /// <param name="element">The source element.</param>
    /// <returns>The attached command parameter.</returns>
    public static object? GetCommandParameter(AvaloniaObject element)
        => element.GetValue(CommandParameterProperty);
}