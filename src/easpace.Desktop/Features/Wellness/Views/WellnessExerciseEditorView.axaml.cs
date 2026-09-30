// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using easpace.Desktop.Features.Wellness.ViewModels;

namespace easpace.Desktop.Features.Wellness.Views;

internal partial class WellnessExerciseEditorView : UserControl
{
    public WellnessExerciseEditorView()
    {
        InitializeComponent();
    }

    // the preview mustn't keep playing unseen, e.g. after navigating to another page
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (DataContext is WellnessExerciseEditorViewModel editor)
        {
            editor.Preview.Pause();
        }
    }

    // the actions open dialogs or change the exercises, so the flyout mustn't stay open over them; a click is handled
    // before its command runs
    private void OnMoreActionClick(object? sender, RoutedEventArgs e) => MoreActionsButton.Flyout?.Hide();
}
