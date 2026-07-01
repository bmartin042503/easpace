using System;
using System.Collections.ObjectModel;

namespace easpace.Desktop.Models.Growth;

public abstract class GrowthActivity 
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public abstract class GrowthActivity<TEntry> : GrowthActivity
    where TEntry : DataEntry
{
    public ObservableCollection<TEntry> Entries { get; set; } = [];
}