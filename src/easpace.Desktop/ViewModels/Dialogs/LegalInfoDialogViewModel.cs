// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.IO;
using Avalonia.Platform;
using easpace.Desktop.Constants;
using easpace.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.ViewModels.Dialogs;

public class LegalInfoDialogViewModel : InfoDialogViewModel
{
    private readonly ILogger<LegalInfoDialogViewModel> _logger;

    public LegalInfoDialogViewModel(ILogger<LegalInfoDialogViewModel> logger, LegalFileType legalFileType)
    {
        _logger = logger;

        Title = legalFileType switch
        {
            LegalFileType.PrivacyPolicy => LocalizationService.GetString("Credits.Text.PrivacyPolicy"),
            LegalFileType.TermsOfUse => LocalizationService.GetString("Credits.Text.TermsOfUse"),
            LegalFileType.AvaloniaLicense => "Avalonia UI • MIT License",
            LegalFileType.DotNetLicense => "CommunityToolkit.Mvvm / Entity Framework Core • MIT License",
            LegalFileType.Sqlite3MultipleCiphersLicense => "SQLite3 Multiple Ciphers • MIT License",
            LegalFileType.DevloopedCredentialManagerLicense => "Devlooped.CredentialManager • MIT License",
            LegalFileType.PhosphorIconsLicense => "Phosphor Icons • MIT License",
            _ => string.Empty
        };

        Message = LoadLegalFile(legalFileType);
    }

    private string LoadLegalFile(LegalFileType legalFileType)
    {
        var currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        var legalFileName = legalFileType switch
        {
            LegalFileType.PrivacyPolicy => "privacy-policy",
            LegalFileType.TermsOfUse => "terms-of-use",
            LegalFileType.AvaloniaLicense => "avalonia",
            LegalFileType.DotNetLicense => "dotnet",
            LegalFileType.Sqlite3MultipleCiphersLicense => "sqlite3-multiple-ciphers",
            LegalFileType.DevloopedCredentialManagerLicense => "devlooped-credential-manager",
            LegalFileType.PhosphorIconsLicense => "phosphor-icons",
            _ => string.Empty
        };

        var legalFileUri = legalFileType is LegalFileType.PrivacyPolicy or LegalFileType.TermsOfUse
            ? new Uri($"avares://easpace.Desktop/Assets/Legal/{legalFileName}-{currentLanguage}.txt")
            : new Uri($"avares://easpace.Desktop/Assets/Legal/ThirdPartyLibs/{legalFileName}-license.txt");

        if (!AssetLoader.Exists(legalFileUri)) return string.Empty;

        try
        {
            using var stream = AssetLoader.Open(legalFileUri);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occurred while loading the legal file");
        }

        return string.Empty;
    }
}