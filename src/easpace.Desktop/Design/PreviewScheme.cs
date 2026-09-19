// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia;
using Avalonia.Controls;
using easpace.Desktop.Services.Presentation;

namespace easpace.Desktop.Design;

/// <summary>
/// Selects a color scheme palette and appearance in the AXAML preview only.
/// Set Variant on the root control being previewed, not on nested reusable views.
/// </summary>
internal sealed class PreviewScheme : AvaloniaObject
{
    public static readonly AttachedProperty<string?> VariantProperty =
        AvaloniaProperty.RegisterAttached<PreviewScheme, Control, string?>("Variant");

    private static Application? _application;
    private static ColorSchemeService? _service;

    static PreviewScheme()
    {
        VariantProperty.Changed.AddClassHandler<Control>((control, _) => Apply(GetVariant(control)));
    }

    public static string? GetVariant(Control control) => control.GetValue(VariantProperty);

    public static void SetVariant(Control control, string? value) => control.SetValue(VariantProperty, value);

    private static void Apply(string? variant)
    {
        if (!Avalonia.Controls.Design.IsDesignMode || variant is null) return;

        var (scheme, appearance) = variant switch
        {
            "HavenBlue.Light" => (ColorScheme.HavenBlue, ColorSchemeAppearance.Light),
            "HavenBlue.Dark" => (ColorScheme.HavenBlue, ColorSchemeAppearance.Dark),
            "AvallamaPurple.Light" => (ColorScheme.AvallamaPurple, ColorSchemeAppearance.Light),
            "AvallamaPurple.Dark" => (ColorScheme.AvallamaPurple, ColorSchemeAppearance.Dark),
            _ => throw new ArgumentException(
                $"Unknown preview variant: '{variant}'. " +
                "Use HavenBlue.Light, HavenBlue.Dark, AvallamaPurple.Light or AvallamaPurple.Dark.",
                nameof(variant))
        };

        var app = Application.Current ?? throw new InvalidOperationException("Application is not initialized.");

        // reuse one service so each change replaces its previous palette
        // a newly created designer Application needs its own service instance
        if (_service is null || !ReferenceEquals(_application, app))
        {
            _application = app;
            _service = new ColorSchemeService();
        }

        _service.SetColorScheme(scheme);
        _service.SetColorSchemeAppearance(appearance);
    }
}