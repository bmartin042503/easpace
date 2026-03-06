using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;

namespace easpace.Desktop.ViewModels;

public partial class IntroViewModel : PageViewModel
{
    private readonly IMessenger _messenger;
    
    public IntroViewModel(IMessenger messenger)
    {
        _messenger = messenger;
        Page = ApplicationPage.Intro;
    }

    [RelayCommand]
    public void Skip()
    {
        _messenger.Send(new ApplicationMessage.RequestPage(ApplicationPage.Journal));
    }
}
