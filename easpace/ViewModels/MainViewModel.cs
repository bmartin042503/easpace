using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Constants;
using easpace.Factories;
using easpace.Services;

namespace easpace.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    private readonly PreferencesService _preferencesService;
    private readonly bool _isNewUser;

    [ObservableProperty] private PageViewModel? _currentPageViewModel;
    [ObservableProperty] private bool _isSidebarVisible = true;
    
    public MainViewModel(
        PageFactory pageFactory,
        PreferencesService preferencesService
    )
    {
        _pageFactory = pageFactory;
        _preferencesService = preferencesService;

        _isNewUser = _preferencesService.ReadPreference<bool>(PreferenceKey.NewUser);
        SetPage(_isNewUser ? ApplicationPage.Intro : ApplicationPage.Journal);
    }

    [RelayCommand]
    public void SetPage(ApplicationPage page)
    {
        if (page != ApplicationPage.Intro)
        {
            if (_isNewUser)
            {
                _preferencesService.SavePreference(PreferenceKey.NewUser, "False");
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