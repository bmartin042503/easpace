// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Constants;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Services.Presentation;

internal interface IToastMessageService
{
    event Action<ToastMessageViewModel?>? ToastMessageRaised;
    
    void ShowToastMessage(string message, ToastMessageType messageType);
}