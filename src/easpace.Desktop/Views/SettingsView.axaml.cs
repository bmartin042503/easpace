// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Views;

internal partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    // a language change only asks for a restart once the settings are left, as it may still be undone until then
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (DataContext is SettingsViewModel settings)
        {
            _ = settings.OfferRestartIfNeededAsync();
        }
    }
}