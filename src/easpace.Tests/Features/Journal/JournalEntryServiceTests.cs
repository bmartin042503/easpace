// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.Services;
using FluentAssertions;

namespace easpace.Tests.Features.Journal;

public class JournalEntryServiceTests
{
    [Fact]
    public void CreateJournalEntry_CreatesAnEntryWithIdentifierAndTimestamp()
    {
        var service = new JournalEntryService();
        var beforeCreate = DateTimeOffset.Now;

        var entry = service.CreateJournalEntry("Morning reflection", "Some content");

        entry.Id.Should().NotBe(Guid.Empty);
        entry.Title.Should().Be("Morning reflection");
        entry.Content.Should().Be("Some content");
        entry.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        service.GetJournalEntries().Should().ContainSingle().Which.Should().BeSameAs(entry);
    }

    [Fact]
    public void GetJournalEntries_ReturnsASnapshotInsteadOfTheInternalList()
    {
        var service = new JournalEntryService();
        service.CreateJournalEntry("Title", "Content");

        var snapshot = service.GetJournalEntries();
        ((List<JournalEntry>)snapshot).Clear();

        service.GetJournalEntries().Should().ContainSingle();
    }

    [Fact]
    public void UpdateJournalEntry_WhenEntryExists_UpdatesAndReturnsIt()
    {
        var service = new JournalEntryService();
        var entry = service.CreateJournalEntry("Old title", "Old content");

        var updated = service.UpdateJournalEntry(entry.Id, "New title", "New content");

        updated.Should().BeSameAs(entry);
        updated.Title.Should().Be("New title");
        updated.Content.Should().Be("New content");
        service.GetJournalEntries().Single().Title.Should().Be("New title");
    }

    [Fact]
    public void UpdateJournalEntry_WhenEntryDoesNotExist_ReturnsNull()
    {
        var service = new JournalEntryService();

        var updated = service.UpdateJournalEntry(Guid.NewGuid(), "Title", "Content");

        updated.Should().BeNull();
    }

    [Fact]
    public void DeleteJournalEntry_WhenEntryExists_RemovesItAndReturnsTrue()
    {
        var service = new JournalEntryService();
        var entry = service.CreateJournalEntry("Title", "Content");

        var deleted = service.DeleteJournalEntry(entry.Id);

        deleted.Should().BeTrue();
        service.GetJournalEntries().Should().BeEmpty();
    }

    [Fact]
    public void DeleteJournalEntry_WhenEntryDoesNotExist_ReturnsFalse()
    {
        var service = new JournalEntryService();

        var deleted = service.DeleteJournalEntry(Guid.NewGuid());

        deleted.Should().BeFalse();
    }
}