using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Constants;
using easpace.Factories;

namespace easpace.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private PageFactory _pageFactory;
    
    [ObservableProperty] private PageViewModel? _currentPageViewModel;
    
    public MainViewModel(
        PageFactory pageFactory
    )
    {
        _pageFactory = pageFactory;
        CurrentPageViewModel = pageFactory.GetPageViewModel(ApplicationPage.Intro);
    }
}