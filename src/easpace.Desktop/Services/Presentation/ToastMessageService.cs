// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using Avalonia.Threading;
using easpace.Desktop.Constants;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Services.Presentation;

internal interface IToastMessageService
{
    event Action<ToastMessageViewModel?>? ToastMessageRaised;

    void ShowToastMessage(string message, ToastMessageType messageType);
}

internal class ToastMessageService : IToastMessageService
{
    public event Action<ToastMessageViewModel?>? ToastMessageRaised;

    // ticks on the UI thread, so a stopped timer can't still hide a newer toast
    private readonly DispatcherTimer _displayTimer;
    private const int DisplayTimeMs = 3000;

    public ToastMessageService()
    {
        _displayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DisplayTimeMs) };
        _displayTimer.Tick += OnDisplayTimerTick;
    }

    public void ShowToastMessage(string message, ToastMessageType messageType)
    {
        var toastMessageViewModel = new ToastMessageViewModel(message, messageType);

        _displayTimer.Stop();

        ToastMessageRaised?.Invoke(toastMessageViewModel);

        _displayTimer.Start();
    }

    private void OnDisplayTimerTick(object? sender, EventArgs e)
    {
        _displayTimer.Stop();

        // hide by raising the event with a null ToastMessageViewModel
        ToastMessageRaised?.Invoke(null);
    }
}
