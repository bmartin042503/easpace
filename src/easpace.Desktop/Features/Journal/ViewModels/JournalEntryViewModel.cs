// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Journal.ViewModels;

internal partial class JournalEntryViewModel : ViewModelBase
{
    public Guid Id { get; }

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _content = string.Empty;
    [ObservableProperty] private DateTimeOffset _createdAt;

    public JournalEntryViewModel(JournalEntry entry)
    {
        Id = entry.Id;
        UpdateFrom(entry);
    }

    public void UpdateFrom(JournalEntry entry)
    {
        if (entry.Id != Id)
        {
            throw new ArgumentException("A different journal entry cannot update this view model.", nameof(entry));
        }

        Title = entry.Title;
        Content = entry.Content;
        CreatedAt = entry.CreatedAt;
    }
}