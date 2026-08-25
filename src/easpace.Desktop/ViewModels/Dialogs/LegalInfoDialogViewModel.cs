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
            _ => string.Empty
        };

        var localizedLegalFileUri = new Uri($"avares://easpace.Desktop/Assets/Legal/{legalFileName}-{currentLanguage}.txt");

        if (!AssetLoader.Exists(localizedLegalFileUri)) return string.Empty;

        try
        {
            using var stream = AssetLoader.Open(localizedLegalFileUri);
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