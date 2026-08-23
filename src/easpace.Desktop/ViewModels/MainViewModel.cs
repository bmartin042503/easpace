// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Factories;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    private readonly IPreferencesService _preferencesService;
    private readonly IDialogService _dialogService;
    private readonly bool _isBoarded;

    [ObservableProperty] private PageViewModel _currentPageViewModel;
    [ObservableProperty] private bool _isSidebarVisible = true;
    [ObservableProperty] private int _selectedNavIndex;
    [ObservableProperty] private DialogViewModel? _currentDialog;
    
    public MainViewModel(
        PageFactory pageFactory,
        IPreferencesService preferencesService,
        IDialogService dialogService,
        IMessenger messenger
    )
    {
        _pageFactory = pageFactory;
        _preferencesService = preferencesService;
        _dialogService = dialogService;

        _dialogService.CurrentDialogChanged += (dialog) =>
        {
            CurrentDialog = dialog;
        };
        
        messenger.Register<ApplicationMessage.RequestPage>(this, (_, msg) =>
        {
            SetPage(msg.Page);
        });
        
        messenger.Register<ApplicationMessage.SidebarVisibility>(this, (_, msg) =>
        {
            IsSidebarVisible = msg.IsVisible;
        });

        _isBoarded = _preferencesService.ReadPreference<bool>(PreferenceKey.Boarded);

        CurrentPageViewModel = _pageFactory.GetPageViewModel(_isBoarded
            ? ApplicationPage.Journal
            : ApplicationPage.Intro);
    }

    [RelayCommand]
    public void SetPage(ApplicationPage page)
    {
        if (CurrentPageViewModel.Page == page) return;
        
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
        
        SelectedNavIndex = page switch
        {
            ApplicationPage.Journal => 0,
            ApplicationPage.Activities => 1,
            ApplicationPage.Mood => 2,
            ApplicationPage.Wellness => 3,
            ApplicationPage.Settings => 4,
            _ => -1
        };
    }
    
    partial void OnSelectedNavIndexChanged(int value)
    {
        var targetPage = value switch
        {
            0 => ApplicationPage.Journal,
            1 => ApplicationPage.Activities,
            2 => ApplicationPage.Mood,
            3 => ApplicationPage.Wellness,
            _ => ApplicationPage.Settings
        };
        
        if (CurrentPageViewModel.Page != targetPage)
        {
            SetPage(targetPage);
        }
    }
}
