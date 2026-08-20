// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Features.Journal.ViewModels;

public partial class JournalEditorViewModel : ViewModelBase
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly Guid? _editingEntryId;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _content = string.Empty;

    public bool IsCreatingNew => !_editingEntryId.HasValue;

    public event EventHandler<JournalEntry>? Saved;
    public event EventHandler? Canceled;

    /// <summary>Creates a blank draft for a new journal entry.</summary>
    public JournalEditorViewModel(IJournalEntryService journalEntryService)
    {
        _journalEntryService = journalEntryService;
        Title = GetFormattedNow();
    }

    /// <summary>Copies an existing entry into an independent editing draft.</summary>
    public JournalEditorViewModel(IJournalEntryService journalEntryService, JournalEntryViewModel entry)
    {
        _journalEntryService = journalEntryService;
        _editingEntryId = entry.Id;
        Title = entry.Title;
        Content = entry.Content;
    }

    [RelayCommand]
    private void Save()
    {
        var title = string.IsNullOrWhiteSpace(Title)
            ? GetFormattedNow()
            : Title.Trim();

        var savedEntry = IsCreatingNew
            ? _journalEntryService.CreateJournalEntry(title, Content)
            : _journalEntryService.UpdateJournalEntry(_editingEntryId!.Value, title, Content);

        // A deleted entry cannot normally be edited, but do not close the editor
        // as if it had been saved if its backing entity no longer exists.
        if (savedEntry is null)
        {
            return;
        }

        Title = savedEntry.Title;
        Saved?.Invoke(this, savedEntry);
    }

    [RelayCommand]
    private void Cancel()
    {
        Canceled?.Invoke(this, EventArgs.Empty);
    }

    private static string GetFormattedNow()
    {
        var culture = CultureInfo.CurrentCulture;
        var dateFormat = culture.TwoLetterISOLanguageName == "hu"
            ? "yyyy. MMMM d. HH:mm"
            : "MMMM d, yyyy, h:mm tt";

        return DateTimeOffset.Now.ToString(dateFormat, culture);
    }
}