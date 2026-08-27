// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Constants;
using easpace.Desktop.Services.Core;

namespace easpace.Desktop.ViewModels.Dialogs;

internal class LegalInfoDialogViewModel : InfoDialogViewModel
{
    public LegalInfoDialogViewModel(
        IApplicationService applicationService,
        LegalFileType legalFileType)
    {
        Title = legalFileType switch
        {
            LegalFileType.PrivacyPolicy => LocalizationService.GetString("Credits.Text.PrivacyPolicy"),
            LegalFileType.TermsOfUse => LocalizationService.GetString("Credits.Text.TermsOfUse"),
            LegalFileType.EaspaceLicense => "easpace • MIT License",
            LegalFileType.AvaloniaLicense => "Avalonia UI • MIT License",
            LegalFileType.DotNetLicense => "CommunityToolkit.Mvvm / Entity Framework Core • MIT License",
            LegalFileType.Sqlite3MultipleCiphersLicense => "SQLite3 Multiple Ciphers • MIT License",
            LegalFileType.DevloopedCredentialManagerLicense => "Devlooped.CredentialManager • MIT License",
            LegalFileType.PhosphorIconsLicense => "Phosphor Icons • MIT License",
            _ => string.Empty
        };

        Message = applicationService.LoadLegalFile(legalFileType);
    }
}