// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
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

    public async Task ShowDialogAsync<TDialogViewModel>(TDialogViewModel dialogViewModel)
        where TDialogViewModel : DialogViewModel
    {
        CurrentDialogChanged?.Invoke(dialogViewModel);
        dialogViewModel.Show();
        await dialogViewModel.WaitAsync();
        CurrentDialogChanged?.Invoke(null);
    }
}