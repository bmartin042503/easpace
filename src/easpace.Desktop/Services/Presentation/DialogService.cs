// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.Services.Presentation;

internal interface IDialogService
{
    event Action<DialogViewModel?>? CurrentDialogChanged;
    
    Task ShowDialogAsync<TDialogViewModel>(TDialogViewModel dialogViewModel)
        where TDialogViewModel : DialogViewModel;
}

internal class DialogService : IDialogService
{
    public event Action<DialogViewModel?>? CurrentDialogChanged;

    // the dialog host shows a single dialog, so overlapping requests are queued
    private readonly SemaphoreSlim _dialogSemaphore = new(1, 1);

    public async Task ShowDialogAsync<TDialogViewModel>(TDialogViewModel dialogViewModel)
        where TDialogViewModel : DialogViewModel
    {
        await _dialogSemaphore.WaitAsync();

        try
        {
            CurrentDialogChanged?.Invoke(dialogViewModel);
            dialogViewModel.Show();
            await dialogViewModel.WaitAsync();
            CurrentDialogChanged?.Invoke(null);
        }
        finally
        {
            _dialogSemaphore.Release();
        }
    }
}