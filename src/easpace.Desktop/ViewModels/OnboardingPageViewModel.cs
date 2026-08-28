// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;

namespace easpace.Desktop.ViewModels;

internal partial class OnboardingPageViewModel : PageViewModel
{
    private readonly IPreferencesService _preferencesService;
    private readonly IMessenger _messenger;

    [ObservableProperty] private OnboardingContent _currentContent;

    [ObservableProperty] private bool _isTermsOfUseAccepted;
    [ObservableProperty] private bool _isBetaSoftwareWarningAccepted;

    [ObservableProperty] private string _legalContent = string.Empty;
    
    private readonly string _termsOfUseContent;
    private readonly string _privacyPolicyContent;
    
    public string VersionText { get; init; }

    public OnboardingPageViewModel(
        IPreferencesService preferencesService,
        IApplicationService applicationService,
        IMessenger messenger)
    {
        _preferencesService = preferencesService;
        _messenger = messenger;
        Page = ApplicationPage.Intro;

        CurrentContent = OnboardingContent.Welcome;

        VersionText = "v" + App.Version.ToString(3);
        
        _termsOfUseContent = applicationService.LoadLegalFile(LegalFileType.TermsOfUse);
        _privacyPolicyContent = applicationService.LoadLegalFile(LegalFileType.PrivacyPolicy);
    }

    // navigation could be done with a stack or separate views/viewmodels
    // but the onboarding logic currently is simple so its kept this way

    [RelayCommand]
    private void Continue()
    {
        switch (CurrentContent)
        {
            case OnboardingContent.Welcome:
                CurrentContent = OnboardingContent.TermsOfUse;
                LegalContent = _termsOfUseContent;
                break;

            case OnboardingContent.TermsOfUse:
                if (IsTermsOfUseAccepted)
                {
                    _preferencesService.SavePreference(PreferenceKey.TermsOfUseAccepted, true);
                    CurrentContent = OnboardingContent.PrivacyPolicy;
                    LegalContent = _privacyPolicyContent;
                }

                break;

            case OnboardingContent.PrivacyPolicy:
                CurrentContent = OnboardingContent.BetaSoftwareWarning;
                break;

            case OnboardingContent.BetaSoftwareWarning:
                if (IsBetaSoftwareWarningAccepted)
                {
                    CurrentContent = OnboardingContent.ThankYou;
                }
                break;
            
            case OnboardingContent.ThankYou:
                _messenger.Send(new ApplicationMessage.RequestPage(ApplicationPage.Journal));
                break;
        }
    }

    [RelayCommand]
    private void NavigateBack()
    {
        switch (CurrentContent)
        {
            case OnboardingContent.TermsOfUse:
                CurrentContent = OnboardingContent.Welcome;
                break;
            
            case OnboardingContent.PrivacyPolicy:
                CurrentContent = OnboardingContent.TermsOfUse;
                LegalContent = _termsOfUseContent;
                break;

            case OnboardingContent.BetaSoftwareWarning:
                CurrentContent = OnboardingContent.PrivacyPolicy;
                LegalContent = _privacyPolicyContent;
                break;
        }
    }
}