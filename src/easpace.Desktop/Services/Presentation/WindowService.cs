// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace easpace.Desktop.Services.Presentation;

internal class WindowService : IWindowService
{
    private WindowState _previousWindowState = WindowState.Normal;

    public void EnterFullScreen()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktopApp)
        {
            _previousWindowState = desktopApp.MainWindow.WindowState;
            desktopApp.MainWindow.WindowState = WindowState.FullScreen;
        }
    }

    public void ExitFullScreen()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktopApp)
        {
            desktopApp.MainWindow.WindowState = _previousWindowState;
        }
    }
}