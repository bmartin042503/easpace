// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class JournalViewModel : PageViewModel
{
    public ObservableCollection<JournalEntry> JournalEntries { get; } = [];

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasSelectedEntry))]
    private JournalEntry? _selectedJournalEntry;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isCreatingNew;
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string _editContent = string.Empty;

    public bool HasEntries => JournalEntries.Count > 0;
    public bool HasSelectedEntry => SelectedJournalEntry != null;

    public JournalViewModel()
    {
        Page = ApplicationPage.Journal;

        JournalEntries.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasEntries));

        SelectedJournalEntry = JournalEntries.FirstOrDefault();
    }

    [RelayCommand]
    private void AddNewEntry()
    {
        var culture = CultureInfo.CurrentCulture;
        
        var dateFormat = culture.TwoLetterISOLanguageName == "hu" 
            ? "yyyy. MMMM d. HH:mm" 
            : "MMMM d, yyyy, h:mm tt";
        
        var newEntry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            Title = DateTime.Now.ToString(dateFormat, culture),
            Content = "",
            CreatedAt = DateTime.Now
        };

        JournalEntries.Insert(0, newEntry);
        SelectedJournalEntry = newEntry;

        EditTitle = newEntry.Title;
        EditContent = newEntry.Content;
        IsCreatingNew = true;
        IsEditing = true;
    }

    [RelayCommand]
    private void EditEntry()
    {
        if (SelectedJournalEntry == null) return;

        IsCreatingNew = false;

        EditTitle = SelectedJournalEntry.Title;
        EditContent = SelectedJournalEntry.Content;
        IsEditing = true;
    }

    [RelayCommand]
    private void SaveEntry()
    {
        if (string.IsNullOrEmpty(EditTitle)) return;
        
        if (SelectedJournalEntry != null)
        {
            SelectedJournalEntry.Title = EditTitle;
            SelectedJournalEntry.Content = EditContent;
        }

        IsEditing = false;
        IsCreatingNew = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        if (IsCreatingNew && SelectedJournalEntry != null)
        {
            JournalEntries.Remove(SelectedJournalEntry);
            SelectedJournalEntry = JournalEntries.FirstOrDefault();
        }

        IsEditing = false;
        IsCreatingNew = false;
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (SelectedJournalEntry == null) return;

        JournalEntries.Remove(SelectedJournalEntry);
        SelectedJournalEntry = JournalEntries.FirstOrDefault();

        IsEditing = false;
        IsCreatingNew = false;
    }
}