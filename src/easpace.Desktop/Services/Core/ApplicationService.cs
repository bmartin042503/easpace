// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Styling;
using easpace.Desktop.Constants;
using easpace.Desktop.Services.Data;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Services.Core;

internal class ApplicationService : IApplicationService
{
    private readonly IPreferencesService _preferencesService;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(
        IPreferencesService preferencesService,
        ILogger<ApplicationService> logger)
    {
        _preferencesService = preferencesService;
        _logger = logger;
    }
    
    public void Restart()
    {
        
#if DEBUG
        Shutdown();
        return;
#endif
        
        var processPath = Environment.ProcessPath;

        // start another instance of the app
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false
        });
        
        Shutdown();
    }

    public void Shutdown()
    {
        // close the current instance of the app
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            desktopApp.Shutdown();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    public void SetThemeVariant(ThemeVariant themeVariant)
    {
        Application.Current?.RequestedThemeVariant = themeVariant;
    }

    public async Task LaunchUriAsync(Uri uri)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
    
            if (topLevel?.Launcher != null)
            {
                await topLevel.Launcher.LaunchUriAsync(uri);
            }
        }
    }
    
    public string LoadLegalFile(LegalFileType legalFileType)
    {
        var currentLanguage = _preferencesService.ReadPreference<string>(PreferenceKey.Language);

        var legalFileName = legalFileType switch
        {
            LegalFileType.PrivacyPolicy => "privacy-policy",
            LegalFileType.TermsOfUse => "terms-of-use",
            LegalFileType.EaspaceLicense => "easpace",
            LegalFileType.AvaloniaLicense => "avalonia",
            LegalFileType.DotNetLicense => "dotnet",
            LegalFileType.Sqlite3MultipleCiphersLicense => "sqlite3-multiple-ciphers",
            LegalFileType.DevloopedCredentialManagerLicense => "devlooped-credential-manager",
            LegalFileType.PhosphorIconsLicense => "phosphor-icons",
            _ => string.Empty
        };

        var legalFileUri = legalFileType is LegalFileType.PrivacyPolicy or LegalFileType.TermsOfUse
            ? new Uri($"avares://easpace.Desktop/Assets/Legal/{legalFileName}-{currentLanguage}.txt")
            : new Uri($"avares://easpace.Desktop/Assets/Legal/Licenses/{legalFileName}-license.txt");

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