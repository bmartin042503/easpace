// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Styling;

namespace easpace.Desktop.Services.Core;

internal interface IApplicationService
{
    void Restart();
    void Shutdown();
    void SetThemeVariant(ThemeVariant themeVariant);
}