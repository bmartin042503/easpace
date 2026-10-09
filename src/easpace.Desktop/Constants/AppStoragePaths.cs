// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.IO;

namespace easpace.Desktop.Constants;

internal static class AppStoragePaths
{
#if DEBUG
    public static string RoamingPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "easpace-dev");

    public static string LocalPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "easpace-dev");
#else
    public static string RoamingPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "easpace");

    public static string LocalPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "easpace");
#endif
}