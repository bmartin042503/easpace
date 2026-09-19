// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using easpace.Desktop.Services.Presentation;

namespace easpace.Desktop.ViewModels;

internal partial class OnboardingPageViewModel : PageViewModel
{
    private readonly IPreferencesService _preferencesService;
    private readonly ITranslucencyService _translucencyService;
    private readonly IColorSchemeService _colorSchemeService;
    private readonly IMessenger _messenger;

    [ObservableProperty] private OnboardingContent _currentContent;

    [ObservableProperty] private bool _isTermsOfUseAccepted;
    [ObservableProperty] private int _selectedColorSchemeIndex;
    [ObservableProperty] private int _selectedColorSchemeAppearanceIndex;
    [ObservableProperty] private bool _isTranslucencyEnabled;
    [ObservableProperty] private bool _isBetaSoftwareWarningAccepted;

    [ObservableProperty] private string _legalContent = string.Empty;

    private readonly string _termsOfUseContent;
    private readonly string _privacyPolicyContent;

    public string VersionText { get; init; }

    public OnboardingPageViewModel(
        IPreferencesService preferencesService,
        IApplicationService applicationService,
        ITranslucencyService translucencyService,
        IColorSchemeService colorSchemeService,
        IMessenger messenger)
    {
        _preferencesService = preferencesService;
        _translucencyService = translucencyService;
        _colorSchemeService = colorSchemeService;
        _messenger = messenger;
        Page = ApplicationPage.Intro;

        CurrentContent = OnboardingContent.Welcome;

        VersionText = "v" + App.Version.ToString(3);

        _termsOfUseContent = applicationService.LoadLegalFile(LegalFileType.TermsOfUse);
        _privacyPolicyContent = applicationService.LoadLegalFile(LegalFileType.PrivacyPolicy);

        PropertyChanged += OnCustomizationPropertyChanged;
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
                    _preferencesService.SavePreference(PreferenceKey.TermsOfUseAcceptedDate, DateTimeOffset.Now);
                    CurrentContent = OnboardingContent.PrivacyPolicy;
                    LegalContent = _privacyPolicyContent;
                }

                break;

            case OnboardingContent.PrivacyPolicy:
                CurrentContent = OnboardingContent.Customize;
                break;

            case OnboardingContent.Customize:
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
                _preferencesService.SavePreference(PreferenceKey.ColorScheme, GetColorScheme());
                _preferencesService.SavePreference(PreferenceKey.ColorSchemeAppearance, GetColorSchemeAppearance());
                _preferencesService.SavePreference(PreferenceKey.Translucency, IsTranslucencyEnabled);
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

            case OnboardingContent.Customize:
                CurrentContent = OnboardingContent.PrivacyPolicy;
                LegalContent = _privacyPolicyContent;
                break;

            case OnboardingContent.BetaSoftwareWarning:
                CurrentContent = OnboardingContent.Customize;
                break;
        }
    }

    private ColorScheme GetColorScheme()
    {
        return SelectedColorSchemeIndex switch
        {
            0 => ColorScheme.HavenBlue,
            1 => ColorScheme.AvallamaPurple,
            _ => ColorScheme.HavenBlue
        };
    }
    
    private ColorSchemeAppearance GetColorSchemeAppearance()
    {
        return SelectedColorSchemeAppearanceIndex switch
        {
            0 => ColorSchemeAppearance.Default,
            1 => ColorSchemeAppearance.Light,
            2 => ColorSchemeAppearance.Dark,
            _ => ColorSchemeAppearance.Default
        };
    }

    private void OnCustomizationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedColorSchemeIndex):
                _colorSchemeService.SetColorScheme(GetColorScheme());
                break;
            
            case nameof(SelectedColorSchemeAppearanceIndex):
                _colorSchemeService.SetColorSchemeAppearance(GetColorSchemeAppearance());
                break;
            
            case nameof(IsTranslucencyEnabled):
                _translucencyService.SetTranslucencyEnabled(IsTranslucencyEnabled);
                break;
        }
    }
}