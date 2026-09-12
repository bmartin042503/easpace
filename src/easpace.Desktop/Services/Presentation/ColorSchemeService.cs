// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace easpace.Desktop.Services.Presentation;

internal enum ColorScheme
{
    HavenBlue,
    AvallamaPurple
}

internal enum AppAppearance
{
    Default,
    Light,
    Dark
}

internal interface IColorSchemeService
{
    void Initialize();
    void SetColorScheme(ColorScheme colorScheme, AppAppearance appAppearance);
}

internal class ColorSchemeService : IColorSchemeService
{
    private IResourceProvider? _activeColorScheme;

    public void Initialize()
    {
        var app = Application.Current ?? throw new InvalidOperationException("Application is not initialized.");

        _activeColorScheme = app.Resources.MergedDictionaries.OfType<ResourceInclude>()
            .FirstOrDefault(x => x.Source?.AbsoluteUri.Contains("/ColorSchemes/") == true);
    }

    public void SetColorScheme(ColorScheme colorScheme, AppAppearance appAppearance)
    {
        var app = Application.Current ?? throw new InvalidOperationException("Application is not initialized.");

        var uri = colorScheme switch
        {
            ColorScheme.HavenBlue => new Uri("avares://easpace.Desktop/Design/ColorSchemes/HavenBlue/ColorScheme.axaml"),
            ColorScheme.AvallamaPurple => new Uri("avares://easpace.Desktop/Design/ColorSchemes/AvallamaPurple/ColorScheme.axaml"),
            _ => throw new ArgumentOutOfRangeException(nameof(colorScheme))
        };

        var newScheme = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);

        if (_activeColorScheme is not null)
        {
            app.Resources.MergedDictionaries.Remove(_activeColorScheme);
        }

        app.Resources.MergedDictionaries.Insert(0, newScheme);
        _activeColorScheme = newScheme;

        app.RequestedThemeVariant = appAppearance switch
        {
            AppAppearance.Light => ThemeVariant.Light,
            AppAppearance.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}