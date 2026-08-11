// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Models.Activities;

public abstract partial class Activity : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    [ObservableProperty] private string _name = string.Empty;
}

public abstract class Activity<TEntry> : Activity
    where TEntry : DataEntry
{
    public ObservableCollection<TEntry> Entries { get; } = [];
    
    protected Activity()
    {
        Entries.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(LastEntry));
            OnCollectionChanged();
        };
    }
    
    public TEntry? LastEntry => Entries.MaxBy(e => e.Timestamp);
    
    protected virtual void OnCollectionChanged() { }
}