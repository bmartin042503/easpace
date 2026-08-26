// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;

namespace easpace.Desktop.Services.Core;

internal class ApplicationService : IApplicationService
{
    public void Restart()
    {
        
#if DEBUG
        Shutdown();
        return;
#endif
        
        var processPath = Environment.ProcessPath;

        // start another instance of the app
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false
        });
        
        Shutdown();
    }

    public void Shutdown()
    {
        // close the current instance of the app
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    public void SetThemeVariant(ThemeVariant themeVariant)
    {
        Application.Current?.RequestedThemeVariant = themeVariant;
    }
}