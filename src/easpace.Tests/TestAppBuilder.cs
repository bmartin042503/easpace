// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(easpace.Tests.TestAppBuilder))]

namespace easpace.Tests;

public class TestAppBuilder
{
    // a bare application, as the app's own one needs the services of the host to start its main window
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Application>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}