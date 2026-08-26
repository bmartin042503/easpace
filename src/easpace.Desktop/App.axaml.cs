// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using easpace.Desktop.Constants;
using easpace.Desktop.Services.Data;
using easpace.Desktop.ViewModels;
using easpace.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace easpace.Desktop;

internal partial class App : Application
{
    public static Version Version = new(0,1,0);
    private static IServiceProvider? _services;

    public static void ConfigureServices(IServiceProvider services)
    {
        _services = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // set color scheme
        var preferencesService = _services?.GetRequiredService<IPreferencesService>();
        var colorScheme = preferencesService?.ReadPreference<string>(PreferenceKey.ColorScheme);
        
        RequestedThemeVariant = colorScheme switch
        {
            "system" => ThemeVariant.Default,
            "light" => ThemeVariant.Light,
            "dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        if (_services == null)
        {
            throw new InvalidOperationException("Services are not initialized.");
        }

        RequestedThemeVariant = ThemeVariant.Light;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}