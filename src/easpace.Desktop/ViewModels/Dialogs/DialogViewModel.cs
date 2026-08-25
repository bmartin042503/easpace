// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.ViewModels.Dialogs;

internal partial class DialogViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isOpen;
    
    private TaskCompletionSource _closeTask = new();

    public async Task WaitAsync()
    {
        await _closeTask.Task;
    }

    public void Show()
    {
        if (_closeTask.Task.IsCompleted)
        {
            _closeTask = new TaskCompletionSource();
        }

        IsOpen = true;
    }

    protected void Close()
    {
        IsOpen = false;
        _closeTask.TrySetResult();
    }
}