// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;
using easpace.Desktop.Models;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using FluentAssertions;
using Moq;

namespace easpace.Tests.ViewModels;

public class JournalViewModelTests
{
    [Fact]
    public void Constructor_WhenInitialized_SetsPageToJournalAndDefaultProperties()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();

        // act
        var sut = new JournalViewModel(mockDialogService.Object);

        // assert
        sut.Page.Should().Be(ApplicationPage.Journal);
        sut.JournalEntries.Should().BeEmpty();
        sut.HasEntries.Should().BeFalse();
        sut.HasSelectedEntry.Should().BeFalse();
        sut.IsEditing.Should().BeFalse();
        sut.IsCreatingNew.Should().BeFalse();
    }

    [Fact]
    public void AddEntryCommand_WhenExecuted_CreatesNewEntryAndEntersEditMode()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        var sut = new JournalViewModel(mockDialogService.Object);

        // act
        sut.AddEntryCommand.Execute(null);

        // assert
        sut.JournalEntries.Should().HaveCount(1);
        sut.SelectedJournalEntry.Should().NotBeNull();
        sut.SelectedJournalEntry.Should().Be(sut.JournalEntries.First());

        sut.EditTitle.Should().NotBeNullOrEmpty();
        sut.EditContent.Should().BeEmpty();

        sut.IsCreatingNew.Should().BeTrue();
        sut.IsEditing.Should().BeTrue();
        sut.HasEntries.Should().BeTrue();
    }

    [Fact]
    public void EditEntryCommand_WhenNoEntrySelected_DoesNothing()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        var sut = new JournalViewModel(mockDialogService.Object)
        {
            SelectedJournalEntry = null
        };

        // act
        sut.EditEntryCommand.Execute(null);

        // assert
        sut.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void EditEntryCommand_WhenEntryIsSelected_PopulatesFieldsAndEntersEditMode()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        var sut = new JournalViewModel(mockDialogService.Object);
        var entry = new JournalEntry { Title = "Test Title", Content = "Test Content" };
        sut.JournalEntries.Add(entry);
        sut.SelectedJournalEntry = entry;

        // act
        sut.EditEntryCommand.Execute(null);

        // assert
        sut.EditTitle.Should().Be("Test Title");
        sut.EditContent.Should().Be("Test Content");
        sut.IsCreatingNew.Should().BeFalse();
        sut.IsEditing.Should().BeTrue();
    }

    [Fact]
    public void SaveEntryCommand_WhenTitleIsEmpty_SetsTitleForTodayAndSaves()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        var sut = new JournalViewModel(mockDialogService.Object);
        sut.AddEntryCommand.Execute(null);

        // act
        sut.EditTitle = string.Empty;
        sut.SaveEntryCommand.Execute(null);

        // assert
        sut.SelectedJournalEntry?.Title.Should().NotBeNullOrWhiteSpace();
        sut.JournalEntries.Should().HaveCount(1);
        sut.IsEditing.Should().BeFalse();
    }

    [Fact]
    public void SaveEntryCommand_WhenDataIsValid_UpdatesEntryAndExitsEditMode()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        var sut = new JournalViewModel(mockDialogService.Object);
        var entry = new JournalEntry { Title = "Old Title", Content = "Old Content" };
        sut.JournalEntries.Add(entry);
        sut.SelectedJournalEntry = entry;

        sut.EditEntryCommand.Execute(null);
        sut.EditTitle = "New Title";
        sut.EditContent = "New Content";

        // act
        sut.SaveEntryCommand.Execute(null);

        // assert
        sut.SelectedJournalEntry.Title.Should().Be("New Title");
        sut.SelectedJournalEntry.Content.Should().Be("New Content");
        sut.IsEditing.Should().BeFalse();
        sut.IsCreatingNew.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_WhenCreatingNew_RemovesDraftAndExitsEditMode()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        var sut = new JournalViewModel(mockDialogService.Object);
        var existingEntry = new JournalEntry { Title = "Existing" };
        sut.JournalEntries.Add(existingEntry);

        sut.AddEntryCommand.Execute(null);

        // act
        sut.CancelCommand.Execute(null);

        // assert
        sut.JournalEntries.Should().HaveCount(1);
        sut.SelectedJournalEntry.Should().Be(existingEntry);
        sut.IsEditing.Should().BeFalse();
        sut.IsCreatingNew.Should().BeFalse();
    }

    [Fact]
    public void CancelCommand_WhenEditingExisting_KeepsEntryUnchangedAndExitsEditMode()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        var sut = new JournalViewModel(mockDialogService.Object);
        var entry = new JournalEntry { Title = "Original Title" };
        sut.JournalEntries.Add(entry);
        sut.SelectedJournalEntry = entry;

        sut.EditEntryCommand.Execute(null);
        sut.EditTitle = "Modified Title";

        // act
        sut.CancelCommand.Execute(null);

        // assert
        sut.JournalEntries.Should().HaveCount(1);
        sut.SelectedJournalEntry.Title.Should().Be("Original Title");
        sut.IsEditing.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenContentIsEmpty_RemovesEntryWithoutAsking()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        
        var sut = new JournalViewModel(mockDialogService.Object);
        var entry = new JournalEntry { Title = "Journal Title", Content = "   " };
        sut.JournalEntries.Add(entry);
        sut.SelectedJournalEntry = entry;
        
        // act
        await sut.DeleteEntryCommand.ExecuteAsync(null);
        
        // assert
        mockDialogService.Verify(x => x.ShowDialogAsync(It.IsAny<ConfirmDialogViewModel>()), Times.Never);
        sut.JournalEntries.Should().HaveCount(0);
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenDialogCancelled_DoesNotRemoveEntry()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        
        mockDialogService
            .Setup(x => x.ShowDialogAsync(It.IsAny<ConfirmDialogViewModel>()))
            .Callback<ConfirmDialogViewModel>(dialog => dialog.Confirmed = false)
            .Returns(Task.CompletedTask);

        var sut = new JournalViewModel(mockDialogService.Object);
        var entry = new JournalEntry { Title = "Journal Title", Content = "Journal Content" };
        sut.JournalEntries.Add(entry);
        sut.SelectedJournalEntry = entry;

        // act
        await sut.DeleteEntryCommand.ExecuteAsync(null);

        // assert
        sut.JournalEntries.Should().HaveCount(1);
        sut.SelectedJournalEntry.Should().Be(entry);
    }

    [Fact]
    public async Task DeleteEntryCommand_WhenDialogConfirmed_RemovesEntryAndSelectsNext()
    {
        // arrange
        var mockDialogService = new Mock<IDialogService>();
        
        mockDialogService
            .Setup(x => x.ShowDialogAsync(It.IsAny<ConfirmDialogViewModel>()))
            .Callback<ConfirmDialogViewModel>(dialog => dialog.Confirmed = true)
            .Returns(Task.CompletedTask);

        var sut = new JournalViewModel(mockDialogService.Object);
        var entry1 = new JournalEntry { Title = "Entry 1" };
        var entry2 = new JournalEntry { Title = "Entry 2" };
        sut.JournalEntries.Add(entry1);
        sut.JournalEntries.Add(entry2);

        sut.SelectedJournalEntry = entry1;

        // act
        await sut.DeleteEntryCommand.ExecuteAsync(null);

        // assert
        sut.JournalEntries.Should().HaveCount(1);
        sut.JournalEntries.Should().NotContain(entry1);
        sut.SelectedJournalEntry.Should().Be(entry2);
    }
}