using CommunityToolkit.Mvvm.Messaging.Messages;

namespace easpace.Desktop.Constants;

public static class ApplicationMessage
{
    // request for an application page
    public class RequestPage(ApplicationPage page) : ValueChangedMessage<bool>(true)
    {
        public ApplicationPage Page { get; } = page;
    }
}
