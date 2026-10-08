// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using easpace.Desktop.Features.Journal.ViewModels;

namespace easpace.Desktop.Features.Journal.Views;

internal partial class JournalPageView : UserControl
{
    private JournalPageViewModel? _viewModel;
    private bool _isRestoring;

    public JournalPageView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            _viewModel = DataContext as JournalPageViewModel;
            if (_viewModel is null) return;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            RestoreOffsets();
        };

        Unloaded += (_, _) =>
        {
            if (_viewModel is not null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = null;
        };

        EntriesScrollViewer.ScrollChanged += (_, _) =>
        {
            if (_isRestoring || _viewModel is null) return;

            _viewModel.EntriesScrollOffset = EntriesScrollViewer.Offset;
        };

        EntryContentScrollViewer.ScrollChanged += (_, _) =>
        {
            if (_isRestoring || !EntryContentScrollViewer.IsEffectivelyVisible) return;

            if (_viewModel?.ActiveEntry is { } entry)
            {
                entry.ScrollOffset = EntryContentScrollViewer.Offset;
            }
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(JournalPageViewModel.ActiveEntry) or nameof(JournalPageViewModel.IsEditing))
        {
            RestoreOffsets();
        }
    }

    private void RestoreOffsets()
    {
        _isRestoring = true;

        // restore after the updated content has been laid out
        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel is null || !IsLoaded) return;

            EntriesScrollViewer.Offset = _viewModel.EntriesScrollOffset;

            if (EntryContentScrollViewer.IsEffectivelyVisible)
            {
                EntryContentScrollViewer.Offset = _viewModel.ActiveEntry?.ScrollOffset ?? default;
            }

            _isRestoring = false;
        }, DispatcherPriority.Loaded);
    }
}