// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Models;

public partial class JournalEntry : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _content = string.Empty;
}
