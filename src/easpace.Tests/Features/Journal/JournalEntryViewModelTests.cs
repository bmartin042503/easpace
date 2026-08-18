// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Journal.Entities;
using easpace.Desktop.Features.Journal.ViewModels;
using FluentAssertions;

namespace easpace.Tests.Features.Journal;

public class JournalEntryViewModelTests
{
    [Fact]
    public void Constructor_CopiesTheEntryValues()
    {
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            CreatedAt = new DateTimeOffset(2026, 8, 18, 10, 30, 0, TimeSpan.Zero),
            Title = "Title",
            Content = "Content",
        };

        var entryVm = new JournalEntryViewModel(entry);

        entryVm.Id.Should().Be(entry.Id);
        entryVm.CreatedAt.Should().Be(entry.CreatedAt);
        entryVm.Title.Should().Be(entry.Title);
        entryVm.Content.Should().Be(entry.Content);
    }

    [Fact]
    public void UpdateFrom_WhenIdsMatch_RefreshesTheDisplayedValues()
    {
        var id = Guid.NewGuid();
        var entryVm = new JournalEntryViewModel(new JournalEntry { Id = id, Title = "Old", Content = "Old content" });
        var updated = new JournalEntry
        {
            Id = id,
            CreatedAt = new DateTimeOffset(2026, 8, 18, 11, 0, 0, TimeSpan.Zero),
            Title = "New",
            Content = "New content",
        };

        entryVm.UpdateFrom(updated);

        entryVm.Title.Should().Be("New");
        entryVm.Content.Should().Be("New content");
        entryVm.CreatedAt.Should().Be(updated.CreatedAt);
    }

    [Fact]
    public void UpdateFrom_WhenIdsDoNotMatch_Throws()
    {
        var entryVm = new JournalEntryViewModel(new JournalEntry { Id = Guid.NewGuid() });
        var anotherEntry = new JournalEntry { Id = Guid.NewGuid() };

        var action = () => entryVm.UpdateFrom(anotherEntry);

        action.Should().Throw<ArgumentException>();
    }
}