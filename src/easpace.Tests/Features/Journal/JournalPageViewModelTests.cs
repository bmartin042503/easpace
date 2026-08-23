// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;
using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Features.Journal.ViewModels;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Dialogs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.Features.Journal;

public class JournalPageViewModelTests
{
    [Fact]
    public void Constructor_InitializesAnEmptyJournalPage()
    {
        var journalPageVm = CreatePage(new Mock<IJournalEntryService>(), out _);

        journalPageVm.Page.Should().Be(ApplicationPage.Journal);
        journalPageVm.Entries.Should().BeEmpty();
        journalPageVm.HasEntries.Should().BeFalse();
        journalPageVm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void AddEntryCommand_OpensEditorWithoutCreatingAPersistedEntry()
    {
        var mockService = new Mock<IJournalEntryService>();
        var journalPageVm = CreatePage(mockService, out _);

        journalPageVm.AddEntryCommand.Execute(null);

        journalPageVm.IsEditing.Should().BeTrue();
        journalPageVm.Editor.Should().NotBeNull();
        journalPageVm.Entries.Should().BeEmpty();
        
        mockService.Verify(s => s.CreateJournalEntryAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NewEntrySave_AddsSelectsAndClosesEditor()
    {
        var mockService = new Mock<IJournalEntryService>();
        var journalPageVm = CreatePage(mockService, out _);
        journalPageVm.AddEntryCommand.Execute(null);

        var newEntry = new JournalEntry { Id = Guid.NewGuid(), Title = "Saved title", Content = "Saved content" };
        mockService.Setup(s => s.CreateJournalEntryAsync("Saved title", "Saved content")).ReturnsAsync(newEntry);

        journalPageVm.Editor!.Title = "Saved title";
        journalPageVm.Editor.Content = "Saved content";
        await journalPageVm.Editor.SaveCommand.ExecuteAsync(null);

        journalPageVm.IsEditing.Should().BeFalse();
        journalPageVm.Editor.Should().BeNull();
        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.SelectedEntry.Should().Be(journalPageVm.Entries.Single());
        journalPageVm.SelectedEntry!.Title.Should().Be("Saved title");
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenContentIsEmpty_DeletesWithoutShowingADialog()
    {
        var mockService = new Mock<IJournalEntryService>();
        var emptyEntry = new JournalEntry { Id = Guid.NewGuid(), Title = "Title", Content = "   " };
        
        mockService.Setup(s => s.GetJournalEntriesAsync()).ReturnsAsync(new List<JournalEntry> { emptyEntry });
        mockService.Setup(s => s.DeleteJournalEntryAsync(emptyEntry.Id)).ReturnsAsync(true);
        
        var journalPageVm = CreatePage(mockService, out var dialogService);
        await journalPageVm.InitializeAsync();

        journalPageVm.SelectedEntry = journalPageVm.Entries.First();

        await journalPageVm.DeleteEntryCommand.ExecuteAsync(null);
        
        dialogService.Verify(s => s.ShowDialogAsync(It.IsAny<DialogViewModel>()), Times.Never);
        mockService.Verify(s => s.DeleteJournalEntryAsync(emptyEntry.Id), Times.Once);

        journalPageVm.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenDialogIsCanceled_KeepsTheEntry()
    {
        var mockService = new Mock<IJournalEntryService>();
        var entry = new JournalEntry { Id = Guid.NewGuid(), Title = "Title", Content = "Meaningful content" };
        
        mockService.Setup(s => s.GetJournalEntriesAsync()).ReturnsAsync(new List<JournalEntry> { entry });
        var sut = CreatePage(mockService, out var dialogService);
        await sut.InitializeAsync();

        sut.SelectedEntry = sut.Entries.First();

        dialogService
            .Setup(s => s.ShowDialogAsync(It.IsAny<DialogViewModel>()))
            .Callback<DialogViewModel>(dialog => ((ConfirmDialogViewModel)dialog).Confirmed = false)
            .Returns(Task.CompletedTask);

        await sut.DeleteEntryCommand.ExecuteAsync(null);

        mockService.Verify(s => s.DeleteJournalEntryAsync(It.IsAny<Guid>()), Times.Never);
        sut.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchText_FiltersByTitleAndContent()
    {
        var mockService = new Mock<IJournalEntryService>();
        var entry1 = new JournalEntry { Id = Guid.NewGuid(), Title = "Morning reflection", Content = "Coffee and a walk" };
        var entry2 = new JournalEntry { Id = Guid.NewGuid(), Title = "Evening note", Content = "Read an excellent book" };
        
        mockService.Setup(s => s.GetJournalEntriesAsync()).ReturnsAsync(new List<JournalEntry> { entry1, entry2 });
        
        var journalPageVm = CreatePage(mockService, out _);
        await journalPageVm.InitializeAsync();

        journalPageVm.SearchText = "book";
        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.Entries.Single().Title.Should().Be("Evening note");

        journalPageVm.SearchText = "morning";
        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.Entries.Single().Title.Should().Be("Morning reflection");
    }

    private static JournalPageViewModel CreatePage(Mock<IJournalEntryService> mockService, out Mock<IDialogService> dialogService)
    {
        dialogService = new Mock<IDialogService>();
        var mockLogger = new Mock<ILogger<JournalPageViewModel>>();
        return new JournalPageViewModel(mockService.Object, dialogService.Object, mockLogger.Object);
    }
}