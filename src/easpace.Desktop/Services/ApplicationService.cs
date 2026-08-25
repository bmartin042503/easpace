// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Services;

internal class ApplicationService(ILogger<ApplicationService> logger) : IApplicationService
{
    public void Restart()
    {
        logger.LogInformation("Restarting application");
        var processPath = Environment.ProcessPath;

        // start another instance of the app
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false
        });
        
        logger.LogInformation("Application restarted with PID: {PID}", process?.Id);
        
        Shutdown();
    }

    public void Shutdown()
    {
        // close the current instance of the app
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
            logger.LogInformation("Application shut down with PID: {PID}", Environment.ProcessId);
        }
        else
        {
            Environment.Exit(0);
        }
    }
}