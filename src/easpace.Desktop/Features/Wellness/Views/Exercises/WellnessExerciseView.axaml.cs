// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Controls;

namespace easpace.Desktop.Features.Wellness.Views.Exercises;

public partial class WellnessExerciseView : UserControl
{
    public WellnessExerciseView()
    {
        InitializeComponent();
    }
    
    private void OnInstructionElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        e.Element.Tag = e.Index + 1;
    }

    private void OnInstructionElementIndexChanged(object? sender, ItemsRepeaterElementIndexChangedEventArgs e)
    {
        e.Element.Tag = e.NewIndex + 1;
    }
}