using System;
using System.Globalization;
using Avalonia.Markup.Xaml;

namespace easpace.Services;

public class LocalizationService : MarkupExtension
{
    private static readonly System.Threading.Lock Lock = new();
    private static CultureInfo _resourceCulture = CultureInfo.CurrentUICulture;
    
    private static System.Resources.ResourceManager ResourceManager {
        get {
            if (field != null) return field;
            lock (Lock)
            {
                field ??= new System.Resources.ResourceManager("easpace.Assets.Localization.Resources",
                    typeof(LocalizationService).Assembly);
            }
            return field;
        }
    }

    public static void ChangeLanguage(CultureInfo cultureInfo)
    {
        _resourceCulture = cultureInfo;
    }
    
    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, _resourceCulture) ?? $"[{key}]";
    }
    
    public string Key { get; set; } = string.Empty;
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return GetString(Key);
    }
}