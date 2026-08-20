// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Mood.Constants;

namespace easpace.Desktop.Features.Mood.ViewModels;

public partial class MoodLabelViewModel : ObservableObject
{
    public MoodLabelState State { get; }
    
    public string Name { get; }

    [ObservableProperty] private bool _isChecked;

    public MoodLabelViewModel(MoodLabelState state, string name)
    {
        State = state;
        Name = name;
    }
}