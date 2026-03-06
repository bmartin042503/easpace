using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Models;

public interface IGrowthTarget
{
    public Guid Id { get; }
    public string Title { get;  }
    public string Unit { get; }
    public IEnumerable<IGrowthTargetEntry> Entries { get; }
    public object? Goal { get; }
}

public partial class GrowthTarget<T> : ObservableObject, IGrowthTarget where T : INumber<T>
{
    [ObservableProperty] private Guid _id = Guid.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _unit = string.Empty;
    [ObservableProperty] private IList<GrowthTargetEntry<T>> _entries = [];
    [ObservableProperty] private T? _goal;
    
    IEnumerable<IGrowthTargetEntry> IGrowthTarget.Entries => Entries;
    object? IGrowthTarget.Goal => Goal;
}
