// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using Avalonia.Styling;
using easpace.Desktop.Constants;

namespace easpace.Desktop.Services.Core;

internal interface IApplicationService
{
    void Restart();
    void Shutdown();
    void SetThemeVariant(ThemeVariant themeVariant);
    Task LaunchUriAsync(Uri uri);
    string LoadLegalFile(LegalFileType legalFileType);
}