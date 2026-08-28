// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Factories;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels.Dialogs;

namespace easpace.Desktop.ViewModels;

internal partial class MainViewModel : ViewModelBase
{
    private readonly PageFactory _pageFactory;
    private readonly IApplicationService _applicationService;
    private readonly IUpdateService _updateService;
    private readonly IPreferencesService _preferencesService;
    private readonly IDialogService _dialogService;
    private readonly IToastMessageService _toastMessageService;
    private readonly bool _isBoarded;

    [ObservableProperty] private PageViewModel _currentPageViewModel;
    [ObservableProperty] private bool _isSidebarVisible = true;
    [ObservableProperty] private int _selectedNavIndex;
    [ObservableProperty] private DialogViewModel? _currentDialog;
    [ObservableProperty] private ToastMessageViewModel? _currentToastMessage;
    [ObservableProperty] private bool _isToastMessageVisible;

    private readonly TaskCompletionSource<bool> _isLoadedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MainViewModel(
        PageFactory pageFactory,
        IApplicationService applicationService,
        IUpdateService updateService,
        IPreferencesService preferencesService,
        IDialogService dialogService,
        IToastMessageService toastMessageService,
        IMessenger messenger)
    {
        _pageFactory = pageFactory;
        _applicationService = applicationService;
        _updateService = updateService;
        _preferencesService = preferencesService;
        _dialogService = dialogService;
        _toastMessageService = toastMessageService;

        _dialogService.CurrentDialogChanged += dialog => { CurrentDialog = dialog; };

        _toastMessageService.ToastMessageRaised += async toastMessage =>
        {
            if (toastMessage != null)
            {
                CurrentToastMessage = toastMessage;
                IsToastMessageVisible = true;
            }
            else
            {
                IsToastMessageVisible = false;

                await Task.Delay(300);

                if (!IsToastMessageVisible)
                {
                    CurrentToastMessage = null;
                }
            }
        };

        messenger.Register<ApplicationMessage.RequestPage>(this, (_, msg) => { SetPage(msg.Page); });

        messenger.Register<ApplicationMessage.SidebarVisibility>(this,
            (_, msg) => { IsSidebarVisible = msg.IsVisible; });

        _isBoarded = _preferencesService.ReadPreference<bool>(PreferenceKey.Boarded);

        IsSidebarVisible = _isBoarded;

        if (_isBoarded)
        {
            _isLoadedTcs.TrySetResult(true);
        }

        CurrentPageViewModel = _pageFactory.GetPageViewModel(_isBoarded
            ? ApplicationPage.Journal
            : ApplicationPage.Intro);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        // wait until main view model sets to a different page than Intro
        await _isLoadedTcs.Task;

        // check for updates if the setting is turned on
        var isCheckForUpdatesSettingOn = _preferencesService.ReadPreference<bool>(PreferenceKey.CheckForUpdates);

        if (!isCheckForUpdatesSettingOn) return;

        var updateCheckResult = await _updateService.CheckForUpdatesAsync();


        if (updateCheckResult.IsUpdateAvailable && updateCheckResult.LatestVersion != null)
        {
            var currentVersion = App.Version.ToString();
            var newVersion = updateCheckResult.LatestVersion.ToString();

            var dialogMessage = string.Format(LocalizationService.GetString("NewUpdate.Dialog.Description"),
                currentVersion, newVersion);

            dialogMessage += "\n\n------\n";

            dialogMessage += $"\n{updateCheckResult.ReleaseTitle}\n";
            dialogMessage += $"{updateCheckResult.ReleaseDescription}\n";

            var confirmDialog = new DetailedConfirmDialogViewModel
            {
                Title = LocalizationService.GetString("NewUpdate.Dialog.Title"),
                Message = dialogMessage,
                ConfirmText = LocalizationService.GetString("Common.Button.Download"),
                CancelText = LocalizationService.GetString("Common.Button.Later")
            };

            await _dialogService.ShowDialogAsync(confirmDialog);

            if (confirmDialog.Confirmed && !string.IsNullOrWhiteSpace(updateCheckResult.ReleaseUrl))
            {
                await _applicationService.LaunchUriAsync(new Uri(updateCheckResult.ReleaseUrl));
            }
        }
    }

    [RelayCommand]
    private void SetPage(ApplicationPage page)
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