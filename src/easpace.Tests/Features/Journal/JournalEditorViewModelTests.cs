// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Features.Journal.ViewModels;
using FluentAssertions;
using Moq;

namespace easpace.Tests.Features.Journal;

public class JournalEditorViewModelTests
{
    [Fact]
    public void NewEditor_StartsAsAnIndependentDraft()
    {
        var mockService = new Mock<IJournalEntryService>();
        var editor = new JournalEditorViewModel(mockService.Object);

        editor.IsCreatingNew.Should().BeTrue();
        editor.Title.Should().NotBeNullOrWhiteSpace();
        editor.Content.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveCommand_WhenCreatingNew_CreatesEntryAndRaisesSaved()
    {
        var mockService = new Mock<IJournalEntryService>();
        var editor = new JournalEditorViewModel(mockService.Object)
        {
            Title = "A clear title",
            Content = "A clear thought",
        };

        var returnedEntry = new JournalEntry { Id = Guid.NewGuid(), Title = "A clear title", Content = "A clear thought" };
        mockService.Setup(s => s.CreateJournalEntryAsync("A clear title", "A clear thought"))
                   .ReturnsAsync(returnedEntry);

        JournalEntry? savedEntry = null;
        editor.Saved += (_, entry) => savedEntry = entry;

        await editor.SaveCommand.ExecuteAsync(null);

        savedEntry.Should().NotBeNull();
        savedEntry.Should().BeSameAs(returnedEntry);
    }

    [Fact]
    public async Task SaveCommand_WhenTitleIsWhitespace_GeneratesADefaultTitle()
    {
        var mockService = new Mock<IJournalEntryService>();
        var editor = new JournalEditorViewModel(mockService.Object)
        {
            Title = "   ",
            Content = "Some content"
        };
        
        mockService.Setup(s => s.CreateJournalEntryAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new JournalEntry());

        await editor.SaveCommand.ExecuteAsync(null);
        
        mockService.Verify(s => s.CreateJournalEntryAsync(
            It.Is<string>(title => !string.IsNullOrWhiteSpace(title)), 
            "Some content"), Times.Once);
    }

    [Fact]
    public void ExistingEditor_DoesNotMutateTheEntryUntilSave()
    {
        var mockService = new Mock<IJournalEntryService>();
        var entry = new JournalEntry { Id = Guid.NewGuid(), Title = "Original", Content = "Original content" };
        var entryViewModel = new JournalEntryViewModel(entry);
        
        var editor = new JournalEditorViewModel(mockService.Object, entryViewModel)
        {
            Title = "Changed",
            Content = "Changed content",
        };

        entryViewModel.Title.Should().Be("Original");
        entryViewModel.Content.Should().Be("Original content");
    }

    [Fact]
    public void CancelCommand_RaisesCanceledWithoutChangingTheService()
    {
        var mockService = new Mock<IJournalEntryService>();
        var editor = new JournalEditorViewModel(mockService.Object);
        var canceled = false;
        editor.Canceled += (_, _) => canceled = true;

        editor.CancelCommand.Execute(null);

        canceled.Should().BeTrue();
        mockService.Verify(s => s.CreateJournalEntryAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}