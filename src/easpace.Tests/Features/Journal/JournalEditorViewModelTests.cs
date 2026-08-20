// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Features.Journal.ViewModels;
using FluentAssertions;

namespace easpace.Tests.Features.Journal;

public class JournalEditorViewModelTests
{
    [Fact]
    public void NewEditor_StartsAsAnIndependentDraft()
    {
        var service = new JournalEntryService();

        var editor = new JournalEditorViewModel(service);

        editor.IsCreatingNew.Should().BeTrue();
        editor.Title.Should().NotBeNullOrWhiteSpace();
        editor.Content.Should().BeEmpty();
        service.GetJournalEntries().Should().BeEmpty();
    }

    [Fact]
    public void SaveCommand_WhenCreatingNew_CreatesEntryAndRaisesSaved()
    {
        var service = new JournalEntryService();
        var editor = new JournalEditorViewModel(service)
        {
            Title = "A clear title",
            Content = "A clear thought",
        };

        JournalEntry? savedEntry = null;
        editor.Saved += (_, entry) => savedEntry = entry;

        editor.SaveCommand.Execute(null);

        savedEntry.Should().NotBeNull();
        savedEntry.Title.Should().Be("A clear title");
        savedEntry.Content.Should().Be("A clear thought");
        service.GetJournalEntries().Should().ContainSingle().Which.Should().BeSameAs(savedEntry);
    }

    [Fact]
    public void SaveCommand_WhenTitleIsWhitespace_GeneratesADefaultTitle()
    {
        var service = new JournalEntryService();
        var editor = new JournalEditorViewModel(service)
        {
            Title = "   ",
        };

        editor.SaveCommand.Execute(null);

        service.GetJournalEntries().Single().Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ExistingEditor_DoesNotMutateTheEntryUntilSave()
    {
        var service = new JournalEntryService();
        var entry = service.CreateJournalEntry("Original", "Original content");
        var entryViewModel = new JournalEntryViewModel(entry);
        var editor = new JournalEditorViewModel(service, entryViewModel)
        {
            Title = "Changed",
            Content = "Changed content",
        };

        entryViewModel.Title.Should().Be("Original");
        entryViewModel.Content.Should().Be("Original content");
        service.GetJournalEntries().Single().Title.Should().Be("Original");
    }

    [Fact]
    public void SaveCommand_WhenEditingExisting_UpdatesServiceAndRaisesSaved()
    {
        var service = new JournalEntryService();
        var entry = service.CreateJournalEntry("Original", "Original content");
        var editor = new JournalEditorViewModel(service, new JournalEntryViewModel(entry))
        {
            Title = "Updated",
            Content = "Updated content",
        };

        JournalEntry? savedEntry = null;
        editor.Saved += (_, saved) => savedEntry = saved;

        editor.SaveCommand.Execute(null);

        savedEntry.Should().BeSameAs(entry);
        entry.Title.Should().Be("Updated");
        entry.Content.Should().Be("Updated content");
    }

    [Fact]
    public void CancelCommand_RaisesCanceledWithoutChangingTheService()
    {
        var service = new JournalEntryService();
        var editor = new JournalEditorViewModel(service);
        var canceled = false;
        editor.Canceled += (_, _) => canceled = true;

        editor.CancelCommand.Execute(null);

        canceled.Should().BeTrue();
        service.GetJournalEntries().Should().BeEmpty();
    }
}