// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace easpace.Desktop.Constants;

internal static class ApplicationMessage
{
    // request for an application page
    public class RequestPage(ApplicationPage page) : ValueChangedMessage<bool>(true)
    {
        public ApplicationPage Page { get; } = page;
    }
    
    // sets the sidebar visibility in MainWindow
    public class SidebarVisibility(bool isVisible) : ValueChangedMessage<bool>(true)
    {
        public bool IsVisible { get; } = isVisible;
    }
}