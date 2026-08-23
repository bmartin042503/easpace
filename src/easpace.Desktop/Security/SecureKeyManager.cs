// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Security.Cryptography;
using GitCredentialManager;

namespace easpace.Desktop.Security;

public class SecureKeyManager
{
    private const string AppNamespace = "easpace_app";
    
    // Windows needs "://" otherwise it won't save the credential
    private const string KeyResource = "easpace://local_sqlite_db";
    private const string KeyAccount = "easpace_user";

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

    private static string GenerateCryptographicKey()
    {
        var bytes = new byte[128];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}