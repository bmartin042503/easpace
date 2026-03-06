using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Models;

public partial class MoodEntry : ObservableObject
{
    [ObservableProperty] private double _moodSliderValue;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTime _date;
    [ObservableProperty] private IList<string> _labels = [];
}
