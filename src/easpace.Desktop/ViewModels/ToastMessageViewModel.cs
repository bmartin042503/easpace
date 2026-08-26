// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;

namespace easpace.Desktop.ViewModels;

internal class ToastMessageViewModel : ViewModelBase
{
    public string Message { get; init; }

    public ToastMessageType MessageType { get; init; }

    public ToastMessageViewModel(string message, ToastMessageType messageType)
    {
        Message = message;
        MessageType = messageType;
    }
}