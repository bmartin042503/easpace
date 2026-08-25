// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace easpace.Desktop.Views;

internal partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void GitHubButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var launcher = TopLevel.GetTopLevel(this)?.Launcher;

        var gitHubUri = new Uri("https://github.com/bmartin042503/easpace");
        if (launcher != null)
        {
            await launcher.LaunchUriAsync(gitHubUri);
        }
    }
}