// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Controls;

namespace easpace.Desktop.Features.Wellness.Views.Exercises;

public partial class WellnessExerciseEditorView : UserControl
{
    public WellnessExerciseEditorView()
    {
        InitializeComponent();
    }

    private void OnInstructionContainerPrepared(object? sender, ContainerPreparedEventArgs e)
    {
        e.Container.Tag = e.Index + 1;
    }

    private void OnInstructionContainerIndexChanged(object? sender, ContainerIndexChangedEventArgs e)
    {
        e.Container.Tag = e.NewIndex + 1;
    }
}
