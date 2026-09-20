// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using easpace.Desktop.Views;

namespace easpace.Desktop.Services.Presentation;

internal interface ITranslucencyService
{
    void SetTranslucencyEnabled(bool enabled);
}

internal sealed class TranslucencyService : ITranslucencyService
{
    private const string PageOpacityKey = "Opacity.Page";
    private const string SidebarOpacityKey = "Opacity.Sidebar";
    private const string CardOpacityKey = "Opacity.Card";

    public void SetTranslucencyEnabled(bool enabled)
    {
        Dispatcher.UIThread.VerifyAccess();

        var app = Application.Current ?? throw new InvalidOperationException("Application is not initialized.");

        if (app.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window })
        {
            throw new InvalidOperationException("Assign desktop.MainWindow before calling SetAppearance.");
        }

        var desiredHints = enabled
            ? new[]
            {
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.Transparent
            }
            : Array.Empty<WindowTransparencyLevel>();

        if (!window.TransparencyLevelHint.SequenceEqual(desiredHints))
        {
            window.TransparencyLevelHint = desiredHints;
        }

        SetOpacity(app, PageOpacityKey, enabled ? 0.8 : 1.0);
        SetOpacity(app, SidebarOpacityKey, enabled ? 0.7 : 1.0);
        SetOpacity(app, CardOpacityKey, enabled ? 0.65 : 1.0);
    }

    private static void SetOpacity(Application app, string key, double value)
    {
        if (app.Resources[key] is not double currentOpacity || Math.Abs(currentOpacity - value) > 0.01)
        {
            app.Resources[key] = value;
        }
    }
}