using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Models;

public partial class MoodLabel : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isChecked = false;
}
