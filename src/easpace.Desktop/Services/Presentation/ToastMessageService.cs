// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Timers;
using Avalonia.Threading;
using easpace.Desktop.Constants;
using easpace.Desktop.ViewModels;

namespace easpace.Desktop.Services.Presentation;

internal class ToastMessageService : IToastMessageService, IDisposable
{
    public event Action<ToastMessageViewModel?>? ToastMessageRaised;

    private readonly Timer _displayTimer;
    private const int DisplayTimeMs = 3000;

    public ToastMessageService()
    {
        _displayTimer = new Timer(DisplayTimeMs);
        _displayTimer.AutoReset = false;
        _displayTimer.Elapsed += OnDisplayTimerElapsed;
    }
    
    public void ShowToastMessage(string message, ToastMessageType messageType)
    {
        var toastMessageViewModel = new ToastMessageViewModel(message, messageType);
        
        _displayTimer.Stop();
        
        Dispatcher.UIThread.Post(() => ToastMessageRaised?.Invoke(toastMessageViewModel));
        
        _displayTimer.Start();
    }
    
    private void OnDisplayTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // hide by raising the event with a null ToastMessageViewModel
        Dispatcher.UIThread.Post(() => ToastMessageRaised?.Invoke(null));
    }
    
    public void Dispose()
    {
        _displayTimer.Elapsed -= OnDisplayTimerElapsed;
        _displayTimer.Dispose();
    }
}