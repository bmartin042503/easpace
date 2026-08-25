// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.Services;

internal interface IDialogService
{
    event Action<DialogViewModel?>? CurrentDialogChanged;
    
    Task ShowDialogAsync<TDialogViewModel>(TDialogViewModel dialogViewModel)
        where TDialogViewModel : DialogViewModel;
}