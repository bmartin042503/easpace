using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Models;

public partial class JournalEntry : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _title = string.Empty;
}
