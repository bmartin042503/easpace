using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class JournalViewModel : PageViewModel
{
    public ObservableCollection<JournalEntry> JournalEntries { get; set; }
    
    [ObservableProperty] private JournalEntry? _selectedJournalEntry;
    
    public JournalViewModel()
    {
        Page = ApplicationPage.Journal;
        
        // adding a few JournalEntries for testing purposes
        JournalEntries = new ObservableCollection<JournalEntry>(
        [
            new JournalEntry { Title = "Journal Entry 1", Id = Guid.NewGuid() },
            new JournalEntry { Title = "Journal Entry 2", Id = Guid.NewGuid()  },
            new JournalEntry { Title = "Journal Entry 3", Id = Guid.NewGuid()  },
            new JournalEntry { Title = "Journal Entry 4", Id = Guid.NewGuid()  },
            new JournalEntry { Title = "Journal Entry 5", Id = Guid.NewGuid()  }
        ]);

        SelectedJournalEntry = JournalEntries[0];
    }

    [RelayCommand]
    public void SelectJournalEntry(object parameter)
    {
        if (parameter is not JournalEntry entry) return;
        if (entry.Id == SelectedJournalEntry?.Id) return;
        SelectedJournalEntry = entry;
    }
}
