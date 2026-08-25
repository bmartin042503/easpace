// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Services;

internal interface IDataWipeService
{
    void DeleteEncryptionKey();
    void DeleteDatabaseFile();
    void DeletePreferencesFile();
}