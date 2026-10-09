// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(easpace.Tests.TestAppBuilder))]

namespace easpace.Tests;

public class TestAppBuilder
{
    // Binding tests do not need the desktop application's service initialization.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Application>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
