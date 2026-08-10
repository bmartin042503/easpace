// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.ViewModels.Dialogs;

public partial class DialogViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isOpen;
    
    protected TaskCompletionSource CloseTask = new TaskCompletionSource();

    public async Task WaitAsync()
    {
        await CloseTask.Task;
    }

    public void Show()
    {
        if (CloseTask.Task.IsCompleted)
        {
            CloseTask = new TaskCompletionSource();
        }

        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
        CloseTask.TrySetResult();
    }
}