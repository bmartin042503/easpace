// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Markup.Xaml;

namespace easpace.Desktop.Services.Core;

internal sealed class LocalizationService : MarkupExtension
{
    // Key naming convention: Category.Component.Function
    
    // Adding a new key:
    // - Use exactly three parts, e.g. "Journal.Error.SaveFailed". Reuse a Common key if the same text already exists there.
    // - Add it to both Resources.resx and Resources.hu.resx, inside the group of its Category, at the same position.
    // - Keep both files line-by-line identical except for the values. Start a new Category with a short comment and separate groups with one empty line.

    private static readonly System.Threading.Lock Lock = new();
    
    private static System.Resources.ResourceManager ResourceManager {
        get {
            if (field != null) return field;
            lock (Lock)
            {
                field ??= new System.Resources.ResourceManager("easpace.Desktop.Assets.Localization.Resources",
                    typeof(LocalizationService).Assembly);
            }
            return field;
        }
    }

    public static void ChangeLanguage(CultureInfo cultureInfo)
    {
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    }
    
    public static string GetString(string key, string? fallback = null)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback ?? $"[{key}]";
    }
    
    public string Key { get; set; } = string.Empty;
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return GetString(Key);
    }
}
