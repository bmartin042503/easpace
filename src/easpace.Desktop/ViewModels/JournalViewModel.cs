// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels;

public partial class JournalViewModel : PageViewModel
{
    private readonly IDialogService _dialogService;

    public ObservableCollection<JournalEntry> JournalEntries { get; } = [];

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasSelectedEntry))]
    private JournalEntry? _selectedJournalEntry;

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isCreatingNew;
    [ObservableProperty] private string _editTitle = string.Empty;
    [ObservableProperty] private string _editContent = string.Empty;

    [ObservableProperty] private DialogViewModel _dialog = new();

    public bool HasEntries => JournalEntries.Count > 0;
    public bool HasSelectedEntry => SelectedJournalEntry != null;

    public JournalViewModel(IDialogService dialogService)
    {
        Page = ApplicationPage.Journal;

        _dialogService = dialogService;

        JournalEntries.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasEntries));

        SelectedJournalEntry = JournalEntries.FirstOrDefault();
    }

    [RelayCommand]
    private void AddEntry()
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
        var culture = CultureInfo.CurrentCulture;

        var dateFormat = culture.TwoLetterISOLanguageName == "hu"
            ? "yyyy. MMMM d. HH:mm"
            : "MMMM d, yyyy, h:mm tt";
        
        if (string.IsNullOrEmpty(EditTitle))
        {
            EditTitle = DateTime.Now.ToString(dateFormat, culture);
        }

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
    private async Task DeleteEntry()
    {
        if (SelectedJournalEntry == null) return;

        if (!string.IsNullOrWhiteSpace(SelectedJournalEntry.Content))
        {
            var confirmDeletionDialog = new ConfirmDialogViewModel
            {
                Title = string.Format(LocalizationService.GetString("Journal.DeleteDialog.Title"),
                    SelectedJournalEntry.Title),
                Message = LocalizationService.GetString("Journal.DeleteDialog.Message"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
                IsDestructive = true,
            };

            await _dialogService.ShowDialogAsync(confirmDeletionDialog);

            if (!confirmDeletionDialog.Confirmed) return;
        }

        JournalEntries.Remove(SelectedJournalEntry);
        SelectedJournalEntry = JournalEntries.FirstOrDefault();

        IsEditing = false;
        IsCreatingNew = false;
    }
}