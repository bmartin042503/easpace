// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace easpace.Tests.ViewModels;

public class SettingsViewModelTests
{
    private const int English = 0;
    private const int Hungarian = 1;

    private readonly Mock<IPreferencesService> _preferencesServiceMock = new();
    private readonly Mock<IApplicationService> _applicationServiceMock = new();
    private readonly Mock<IDialogService> _dialogServiceMock = new();

    // the app runs in Hungarian; the saved language is read back like the preferences service would
    private string _savedLanguage = "hu";

    public SettingsViewModelTests()
    {
        _preferencesServiceMock
            .Setup(p => p.ReadPreference(PreferenceKey.Language, It.IsAny<string>()))
            .Returns(() => _savedLanguage);
        _preferencesServiceMock
            .Setup(p => p.SavePreference(PreferenceKey.Language, It.IsAny<string>()))
            .Callback<string, string>((_, language) => _savedLanguage = language);
    }

    private SettingsViewModel CreateViewModel() => new(
        _preferencesServiceMock.Object,
        new Mock<ITranslucencyService>().Object,
        new Mock<IColorSchemeService>().Object,
        _applicationServiceMock.Object,
        new Mock<IDataWipeService>().Object,
        _dialogServiceMock.Object,
        new Mock<IToastMessageService>().Object,
        new Mock<ILogger<SettingsViewModel>>().Object);

    private void AnswerRestart(bool restart) =>
        _dialogServiceMock
            .Setup(d => d.ShowDialogAsync(It.IsAny<ConfirmDialogViewModel>()))
            .Callback<ConfirmDialogViewModel>(dialog =>
            {
                if (restart) dialog.ConfirmCommand.Execute(null);
                else dialog.CancelCommand.Execute(null);
            })
            .Returns(Task.CompletedTask);

    private void VerifyRestartOffered(Times times) =>
        _dialogServiceMock.Verify(d => d.ShowDialogAsync(It.Is<ConfirmDialogViewModel>(dialog =>
            dialog.Title == LocalizationService.GetString("Settings.RestartDialog.Title"))), times);

    [Fact]
    public void ChangingTheLanguage_SavesItWithoutOfferingARestartYet()
    {
        var settings = CreateViewModel();

        settings.SelectedLanguageIndex = English;

        _savedLanguage.Should().Be("en");
        VerifyRestartOffered(Times.Never());
    }

    [Fact]
    public async Task LeavingWithAnotherLanguage_OffersARestart()
    {
        var settings = CreateViewModel();
        settings.SelectedLanguageIndex = English;

        await settings.OfferRestartIfNeededAsync();

        VerifyRestartOffered(Times.Once());
    }

    [Fact]
    public async Task LeavingAfterChangingTheLanguageBack_DoesNotOfferARestart()
    {
        var settings = CreateViewModel();
        settings.SelectedLanguageIndex = English;
        settings.SelectedLanguageIndex = Hungarian;

        await settings.OfferRestartIfNeededAsync();

        VerifyRestartOffered(Times.Never());
    }

    [Fact]
    public async Task LeavingAfterOtherChanges_DoesNotOfferARestart()
    {
        var settings = CreateViewModel();
        settings.IsTranslucencyEnabled = true;

        await settings.OfferRestartIfNeededAsync();

        VerifyRestartOffered(Times.Never());
    }

    [Fact]
    public async Task ConfirmingTheRestart_RestartsTheApp()
    {
        AnswerRestart(restart: true);
        var settings = CreateViewModel();
        settings.SelectedLanguageIndex = English;

        await settings.OfferRestartIfNeededAsync();

        _applicationServiceMock.Verify(a => a.Restart(), Times.Once);
    }

    [Fact]
    public async Task PostponedRestart_IsOfferedAgainOnlyAfterAnotherChangeThatNeedsIt()
    {
        AnswerRestart(restart: false);
        var settings = CreateViewModel();
        settings.SelectedLanguageIndex = English;
        await settings.OfferRestartIfNeededAsync();

        // left again without changing the language
        await settings.OfferRestartIfNeededAsync();

        VerifyRestartOffered(Times.Once());

        // back to the language the app runs with, so there is nothing to restart for
        settings.SelectedLanguageIndex = Hungarian;
        await settings.OfferRestartIfNeededAsync();

        VerifyRestartOffered(Times.Once());
        _applicationServiceMock.Verify(a => a.Restart(), Times.Never);
    }
}
