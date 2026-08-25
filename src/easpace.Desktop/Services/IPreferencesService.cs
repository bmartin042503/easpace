// Copyright (c) 2025-2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Services;

internal interface IPreferencesService
{
    T ReadPreference<T>(string key, T defaultValue = default!);
    void SavePreference<T>(string key, T value);
}