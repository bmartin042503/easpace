// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Security.Cryptography;
using GitCredentialManager;

namespace easpace.Desktop.Security;

internal static class SecureKeyManager
{
    // Windows needs "://" otherwise it won't save the credential

#if DEBUG
    private const string AppNamespace = "easpace_dev_app";
    private const string KeyResource = "easpace_dev://local_sqlite_db";
    private const string KeyAccount = "easpace_dev_user";
#else
    private const string AppNamespace = "easpace_app";
    private const string KeyResource = "easpace://local_sqlite_db";
    private const string KeyAccount = "easpace_user";
#endif

    public static string GetOrGenerateDbPassword()
    {
        var store = CredentialManager.Create(AppNamespace);

        var cred = store.Get(KeyResource, KeyAccount);

        if (cred != null && !string.IsNullOrWhiteSpace(cred.Password))
        {
            return cred.Password;
        }

        var newPassword = GenerateCryptographicKey();

        store.AddOrUpdate(KeyResource, KeyAccount, newPassword);

        return newPassword;
    }

    public static void DeleteDbPassword()
    {
        var store = CredentialManager.Create(AppNamespace);
        store.Remove(KeyResource, KeyAccount);
    }

    private static string GenerateCryptographicKey()
    {
        var bytes = new byte[128];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}