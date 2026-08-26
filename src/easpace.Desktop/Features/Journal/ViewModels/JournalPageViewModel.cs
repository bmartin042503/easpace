// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Journal.ViewModels;

internal partial class JournalPageViewModel : PageViewModel
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<JournalPageViewModel> _logger;
    private readonly ILogger<JournalEditorViewModel> _editorLogger;
    private readonly List<JournalEntryViewModel> _allEntries = [];

    public AvaloniaList<JournalEntryViewModel> Entries { get; } = [];

    [ObservableProperty] private JournalEntryViewModel? _selectedEntry;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsEditing))]
    private JournalEditorViewModel? _editor;

    [NotifyPropertyChangedFor(nameof(HasActiveEntry))] [ObservableProperty]
    private JournalEntryViewModel? _activeEntry;

    public bool HasEntries => _allEntries.Count > 0;
    public bool HasActiveEntry => ActiveEntry is not null;
    public bool IsEditing => Editor is not null;
    
    private bool _isInitialized;

    public JournalPageViewModel(
        IJournalEntryService journalEntryService, 
        IDialogService dialogService, 
        ILogger<JournalPageViewModel> logger,
        ILogger<JournalEditorViewModel> editorLogger)
    {
        _journalEntryService = journalEntryService;
        _dialogService = dialogService;
        _logger = logger;
        _editorLogger = editorLogger;

        Page = ApplicationPage.Journal;
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedEntryChanged(JournalEntryViewModel? value)
    {
        if (value is not null)
        {
            ActiveEntry = value;
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            await LoadEntries();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize journal page and load entries");
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Journal.Error.LoadFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    [RelayCommand]
    private void AddEntry()
    {
        OpenEditor();
    }

    [RelayCommand]
    private void EditEntry()
    {
        if (ActiveEntry is null) return;

        OpenEditor(ActiveEntry);
    }

    [RelayCommand]
    private async Task DeleteEntry()
    {
        var entryToDelete = ActiveEntry;
        if (entryToDelete is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(entryToDelete.Content))
        {
            var confirmation = new ConfirmDialogViewModel
            {
                Title = string.Format(
                    LocalizationService.GetString("Journal.DeleteDialog.Title"),
                    entryToDelete.Title),
                Message = LocalizationService.GetString("Journal.DeleteDialog.Message"),
                CancelText = LocalizationService.GetString("Common.Button.Cancel"),
                ConfirmText = LocalizationService.GetString("Common.Button.Delete"),
                IsDestructive = true,
            };

            await _dialogService.ShowDialogAsync(confirmation);

            if (!confirmation.Confirmed) return;
        }

        try
        {
            var isDeleted = await _journalEntryService.DeleteJournalEntryAsync(entryToDelete.Id);
        
            if (!isDeleted) return;

            _allEntries.Remove(entryToDelete);
            OnPropertyChanged(nameof(HasEntries));

            if (ActiveEntry?.Id == entryToDelete.Id)
            {
                ActiveEntry = null;
            }

            ApplyFilter();
            SelectedEntry = Entries.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while attempting to delete journal entry {EntryId}", entryToDelete.Id);
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Journal.Error.DeleteFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
    }

    private async Task LoadEntries()
    {
        _allEntries.Clear();
        
        var journalEntries = await _journalEntryService.GetJournalEntriesAsync();

        var journalEntryViewModels = journalEntries.Select(entry => new JournalEntryViewModel(entry));

        _allEntries.AddRange(journalEntryViewModels);

        ApplyFilter();
        
        OnPropertyChanged(nameof(HasEntries));
    }

    private void OpenEditor(JournalEntryViewModel? entry = null)
    {
        if (Editor is not null)
        {
            return;
        }

        var editor = entry is null
            ? new JournalEditorViewModel(_journalEntryService, _dialogService, _editorLogger)
            : new JournalEditorViewModel(_journalEntryService, _dialogService, entry, _editorLogger);

        editor.Saved += OnEditorSaved;
        editor.Canceled += OnEditorCanceled;

        Editor = editor;
    }

    private void OnEditorSaved(object? sender, JournalEntry savedEntry)
    {
        var entryViewModel = _allEntries.FirstOrDefault(entry => entry.Id == savedEntry.Id);

        if (entryViewModel is null)
        {
            entryViewModel = new JournalEntryViewModel(savedEntry);
            _allEntries.Insert(0, entryViewModel);
            OnPropertyChanged(nameof(HasEntries));
        }
        else
        {
            entryViewModel.UpdateFrom(savedEntry);
        }

        CloseEditor();

        ActiveEntry = entryViewModel;

        ApplyFilter();

        if (Entries.Contains(entryViewModel))
        {
            SelectedEntry = entryViewModel;
        }
    }

    private void OnEditorCanceled(object? sender, EventArgs eventArgs)
    {
        CloseEditor();
    }

    private void CloseEditor()
    {
        if (Editor is not { } editor)
        {
            return;
        }

        editor.Saved -= OnEditorSaved;
        editor.Canceled -= OnEditorCanceled;

        Editor = null;
    }

    private void ApplyFilter()
    {
        var lastSelectedEntryId = ActiveEntry?.Id;

        var filteredEntries = _allEntries
            .Where(MatchesSearch)
            .ToList();

        Entries.Clear();
        Entries.AddRange(filteredEntries);

        if (lastSelectedEntryId is not null)
        {
            SelectedEntry = Entries.FirstOrDefault(entry => entry.Id == lastSelectedEntryId);
        }
    }

    private bool MatchesSearch(JournalEntryViewModel entry)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return entry.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
               || entry.Content.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);
    }
}