// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;

namespace easpace.Desktop.Services.Presentation;

internal enum ColorScheme
{
    HavenBlue,
    AvallamaPurple
}

internal enum ColorSchemeAppearance
{
    Default,
    Light,
    Dark
}

internal interface IColorSchemeService
{
    void SetColorScheme(ColorScheme colorScheme);
    void SetColorSchemeAppearance(ColorSchemeAppearance colorSchemeAppearance);
}

internal class ColorSchemeService : IColorSchemeService
{
    private ResourceDictionary? _activePalette;
    private ColorScheme? _activeColorScheme;

    public void SetColorScheme(ColorScheme colorScheme)
    {
        Dispatcher.UIThread.VerifyAccess();

        var app = Application.Current ?? throw new InvalidOperationException("Application is not initialized.");

        var uri = colorScheme switch
        {
            ColorScheme.HavenBlue => new Uri("avares://easpace.Desktop/Design/ColorSchemes/HavenBlue.axaml"),
            ColorScheme.AvallamaPurple => new Uri("avares://easpace.Desktop/Design/ColorSchemes/AvallamaPurple.axaml"),
            _ => throw new ArgumentOutOfRangeException(nameof(colorScheme))
        };

        if (_activeColorScheme != colorScheme)
        {
            var newPalette = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
            
            var dictionaries = app.Resources.MergedDictionaries;

            if (_activePalette is null)
            {
                dictionaries.Add(newPalette);
            }
            else
            {
                dictionaries[dictionaries.IndexOf(_activePalette)] = newPalette;
            }

            _activePalette = newPalette;
            _activeColorScheme = colorScheme;
        }
    }
    
    public void SetColorSchemeAppearance(ColorSchemeAppearance colorSchemeAppearance)
    {
        Dispatcher.UIThread.VerifyAccess();

        var app = Application.Current ?? throw new InvalidOperationException("Application is not initialized.");

        var themeVariant = colorSchemeAppearance switch
        {
            ColorSchemeAppearance.Default => ThemeVariant.Default,
            ColorSchemeAppearance.Light => ThemeVariant.Light,
            ColorSchemeAppearance.Dark => ThemeVariant.Dark,

            _ => throw new ArgumentOutOfRangeException(nameof(colorSchemeAppearance))
        };

        app.RequestedThemeVariant = themeVariant;
    }
}