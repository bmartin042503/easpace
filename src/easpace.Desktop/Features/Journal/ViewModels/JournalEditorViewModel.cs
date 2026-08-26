// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Journal.ViewModels;

internal partial class JournalEditorViewModel : ViewModelBase
{
    private readonly IJournalEntryService _journalEntryService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<JournalEditorViewModel> _logger;
    private readonly Guid? _editingEntryId;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _content = string.Empty;

    public bool IsCreatingNew => !_editingEntryId.HasValue;

    public event EventHandler<JournalEntry>? Saved;
    public event EventHandler? Canceled;

    /// <summary>Creates a blank draft for a new journal entry.</summary>
    public JournalEditorViewModel(
        IJournalEntryService journalEntryService,
        IDialogService dialogService,
        ILogger<JournalEditorViewModel> logger)
    {
        _journalEntryService = journalEntryService;
        _dialogService = dialogService;
        _logger = logger;
        Title = GetFormattedNow();
    }

    /// <summary>Copies an existing entry into an independent editing draft.</summary>
    public JournalEditorViewModel(
        IJournalEntryService journalEntryService,
        IDialogService dialogService,
        JournalEntryViewModel entry,
        ILogger<JournalEditorViewModel> logger)
    {
        _journalEntryService = journalEntryService;
        _dialogService = dialogService;
        _logger = logger;
        _editingEntryId = entry.Id;
        Title = entry.Title;
        Content = entry.Content;
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            var title = string.IsNullOrWhiteSpace(Title)
                ? GetFormattedNow()
                : Title.Trim();
            
            if (IsCreatingNew)
            {
                _logger.LogInformation("Saving new journal entry from editor");
            }
            else
            {
                _logger.LogInformation("Updating existing journal entry {Id} from editor", _editingEntryId);
            }

            var savedEntry = IsCreatingNew
                ? await _journalEntryService.CreateJournalEntryAsync(title, Content)
                : await _journalEntryService.UpdateJournalEntryAsync(_editingEntryId!.Value, title, Content);

            // A deleted entry cannot normally be edited, but do not close the editor
            // as if it had been saved if its backing entity no longer exists.
            if (savedEntry is null)
            {
                return;
            }

            Title = savedEntry.Title;
            Saved?.Invoke(this, savedEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save journal entry from editor (IsCreatingNew: {IsCreatingNew})", IsCreatingNew);
            
            var errorDialog = new ErrorDialogViewModel
            {
                Title = LocalizationService.GetString("Common.Error.Title"),
                Message = LocalizationService.GetString("Journal.Error.SaveFailed")
            };
            
            await _dialogService.ShowDialogAsync(errorDialog);
        }
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