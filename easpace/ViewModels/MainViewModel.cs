using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Constants;
using easpace.Factories;
using easpace.Services;

namespace easpace.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    private readonly PreferencesService _preferencesService;
    private readonly IMessenger _messenger;
    private readonly bool _isBoarded;

    [ObservableProperty] private PageViewModel? _currentPageViewModel;
    [ObservableProperty] private bool _isSidebarVisible = true;
    
    public MainViewModel(
        PageFactory pageFactory,
        PreferencesService preferencesService,
        IMessenger messenger
    )
    {
        _pageFactory = pageFactory;
        _preferencesService = preferencesService;
        _messenger = messenger;

        _messenger.Register<ApplicationMessage.RequestPage>(this, (_, msg) =>
        {
            SetPage(msg.Page);
        });

        _isBoarded = _preferencesService.ReadPreference<bool>(PreferenceKey.Boarded);
        SetPage(_isBoarded ? ApplicationPage.Journal : ApplicationPage.Intro);
    }

    [RelayCommand]
    public void SetPage(ApplicationPage page)
    {
        if (page != ApplicationPage.Intro)
        {
            if (!_isBoarded)
            {
                _preferencesService.SavePreference(PreferenceKey.Boarded, true);
            }

            IsSidebarVisible = true;
        }
        else
        {
            IsSidebarVisible = false;
        }
        CurrentPageViewModel = _pageFactory.GetPageViewModel(page);
    }
}