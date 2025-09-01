using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Constants;
using easpace.Factories;

namespace easpace.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    
    [ObservableProperty] private PageViewModel? _currentPageViewModel;
    [ObservableProperty] private bool _isSidebarVisible = true;
    
    public MainViewModel(
        PageFactory pageFactory
    )
    {
        _pageFactory = pageFactory;
        CurrentPageViewModel = pageFactory.GetPageViewModel(ApplicationPage.Intro);
    }

    [RelayCommand]
    public void GoHome()
    {
        IsSidebarVisible = true;
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Mood);
    }

    [RelayCommand]
    public void SetPage(ApplicationPage page)
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(page);
    }
}