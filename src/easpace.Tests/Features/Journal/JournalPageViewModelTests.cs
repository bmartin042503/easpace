// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Features.Journal.ViewModels;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Dialogs;
using FluentAssertions;
using Moq;

namespace easpace.Tests.Features.Journal;

public class JournalPageViewModelTests
{
    [Fact]
    public void Constructor_WhenServiceIsEmpty_InitializesAnEmptyJournalPage()
    {
        var journalPageVm = CreatePage(new JournalService(), out _);

        journalPageVm.Page.Should().Be(ApplicationPage.Journal);
        journalPageVm.Entries.Should().BeEmpty();
        journalPageVm.HasEntries.Should().BeFalse();
        journalPageVm.HasActiveEntry.Should().BeFalse();
        journalPageVm.IsEditing.Should().BeFalse();
        journalPageVm.Editor.Should().BeNull();
    }

    [Fact]
    public void AddEntryCommand_OpensEditorWithoutCreatingAPersistedEntry()
    {
        var service = new JournalService();
        var journalPageVm = CreatePage(service, out _);

        journalPageVm.AddEntryCommand.Execute(null);

        journalPageVm.IsEditing.Should().BeTrue();
        journalPageVm.Editor.Should().NotBeNull();
        journalPageVm.Entries.Should().BeEmpty();
        service.GetJournalEntries().Should().BeEmpty();
    }

    [Fact]
    public void NewEntrySave_AddsSelectsAndClosesEditor()
    {
        var service = new JournalService();
        var journalPageVm = CreatePage(service, out _);
        journalPageVm.AddEntryCommand.Execute(null);

        journalPageVm.Editor!.Title = "Saved title";
        journalPageVm.Editor.Content = "Saved content";
        journalPageVm.Editor.SaveCommand.Execute(null);

        journalPageVm.IsEditing.Should().BeFalse();
        journalPageVm.Editor.Should().BeNull();
        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.SelectedEntry.Should().Be(journalPageVm.Entries.Single());
        journalPageVm.SelectedEntry!.Title.Should().Be("Saved title");
        journalPageVm.SelectedEntry!.Content.Should().Be("Saved content");
        service.GetJournalEntries().Single().Title.Should().Be("Saved title");
        service.GetJournalEntries().Single().Content.Should().Be("Saved content");
    }

    [Fact]
    public void NewEntryCancel_ClosesEditorWithoutAddingAnEntry()
    {
        var service = new JournalService();
        service.CreateJournalEntry("Existing", "Existing content");
        var journalPageVm = CreatePage(service, out _);
        journalPageVm.SelectedEntry = journalPageVm.Entries.First();
        var existingEntry = journalPageVm.SelectedEntry;
        journalPageVm.AddEntryCommand.Execute(null);

        journalPageVm.Editor!.CancelCommand.Execute(null);

        journalPageVm.IsEditing.Should().BeFalse();
        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.SelectedEntry.Should().BeSameAs(existingEntry);
        service.GetJournalEntries().Should().ContainSingle();
    }

    [Fact]
    public void EditEntryCommand_WhenNothingIsSelected_DoesNothing()
    {
        var journalPageVm = CreatePage(new JournalService(), out _);

        journalPageVm.EditEntryCommand.Execute(null);

        journalPageVm.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void ExistingEntryCancel_LeavesTheDisplayedAndPersistedEntryUnchanged()
    {
        var service = new JournalService();
        service.CreateJournalEntry("Original", "Original content");
        var journalPageVm = CreatePage(service, out _);

        journalPageVm.SelectedEntry = journalPageVm.Entries.First();

        journalPageVm.EditEntryCommand.Execute(null);
        journalPageVm.Editor!.Title = "Changed";
        journalPageVm.Editor.Content = "Changed content";
        journalPageVm.Editor.CancelCommand.Execute(null);

        journalPageVm.IsEditing.Should().BeFalse();
        journalPageVm.SelectedEntry!.Title.Should().Be("Original");
        journalPageVm.SelectedEntry.Content.Should().Be("Original content");
        service.GetJournalEntries().Single().Title.Should().Be("Original");
        service.GetJournalEntries().Single().Content.Should().Be("Original content");
    }

    [Fact]
    public void ExistingEntrySave_UpdatesTheActiveEntryAndClosesEditor()
    {
        var service = new JournalService();
        service.CreateJournalEntry("Original", "Original content");

        var journalPageVm = CreatePage(service, out _);

        journalPageVm.SelectedEntry = journalPageVm.Entries.First();

        journalPageVm.EditEntryCommand.Execute(null);
        journalPageVm.Editor!.Title = "Updated";
        journalPageVm.Editor.Content = "Updated content";
        journalPageVm.Editor.SaveCommand.Execute(null);

        journalPageVm.IsEditing.Should().BeFalse();

        journalPageVm.ActiveEntry.Should().NotBeNull();
        journalPageVm.ActiveEntry!.Title.Should().Be("Updated");
        journalPageVm.ActiveEntry.Content.Should().Be("Updated content");

        service.GetJournalEntries().Single().Title.Should().Be("Updated");
        service.GetJournalEntries().Single().Content.Should().Be("Updated content");
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenContentIsEmpty_DeletesWithoutShowingADialog()
    {
        var service = new JournalService();
        service.CreateJournalEntry("Title", "   ");

        var journalPageVm = CreatePage(service, out var dialogService);

        journalPageVm.SelectedEntry = journalPageVm.Entries.First();

        await journalPageVm.DeleteEntryCommand.ExecuteAsync(null);

        dialogService.Verify(
            s => s.ShowDialogAsync(It.IsAny<DialogViewModel>()),
            Times.Never);

        journalPageVm.Entries.Should().BeEmpty();
        journalPageVm.SelectedEntry.Should().BeNull();
        journalPageVm.ActiveEntry.Should().BeNull();

        service.GetJournalEntries().Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenDialogIsCanceled_KeepsTheEntry()
    {
        var service = new JournalService();
        service.CreateJournalEntry("Title", "Meaningful content");
        var sut = CreatePage(service, out var dialogService);

        sut.SelectedEntry = sut.Entries.First();

        dialogService
            .Setup(s => s.ShowDialogAsync(It.IsAny<DialogViewModel>()))
            .Callback<DialogViewModel>(dialog => ((ConfirmDialogViewModel)dialog).Confirmed = false)
            .Returns(Task.CompletedTask);

        await sut.DeleteEntryCommand.ExecuteAsync(null);

        sut.Entries.Should().ContainSingle();
        sut.SelectedEntry.Should().NotBeNull();
        service.GetJournalEntries().Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenDialogIsConfirmed_DeletesAndSelectsAnotherVisibleEntry()
    {
        var service = new JournalService();
        service.CreateJournalEntry("First", "First content");
        service.CreateJournalEntry("Second", "Second content");
        var journalPageVm = CreatePage(service, out var dialogService);

        journalPageVm.SelectedEntry = journalPageVm.Entries.First();
        var entryToDelete = journalPageVm.SelectedEntry;

        dialogService
            .Setup(s => s.ShowDialogAsync(It.IsAny<DialogViewModel>()))
            .Callback<DialogViewModel>(dialog => ((ConfirmDialogViewModel)dialog).Confirmed = true)
            .Returns(Task.CompletedTask);

        await journalPageVm.DeleteEntryCommand.ExecuteAsync(null);

        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.Entries.Should().NotContain(entry => entry.Id == entryToDelete!.Id);
        journalPageVm.SelectedEntry.Should().NotBeNull();
        service.GetJournalEntries().Should().ContainSingle();
    }

    [Fact]
    public void SearchText_FiltersByTitleAndContent()
    {
        var service = new JournalService();
        service.CreateJournalEntry("Morning reflection", "Coffee and a walk");
        service.CreateJournalEntry("Evening note", "Read an excellent book");
        var journalPageVm = CreatePage(service, out _);

        journalPageVm.SearchText = "book";

        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.Entries.Single().Title.Should().Be("Evening note");

        journalPageVm.SearchText = "morning";

        journalPageVm.Entries.Should().ContainSingle();
        journalPageVm.Entries.Single().Title.Should().Be("Morning reflection");
    }

    private static JournalPageViewModel CreatePage(
        IJournalService journalService,
        out Mock<IDialogService> dialogService)
    {
        dialogService = new Mock<IDialogService>();
        return new JournalPageViewModel(journalService, dialogService.Object);
    }
}