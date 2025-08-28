using System;
using System.Globalization;
using Avalonia.Markup.Xaml;

namespace easpace.Services;

public class LocalizationService : MarkupExtension
{
    private static readonly System.Threading.Lock Lock = new();
    private static System.Resources.ResourceManager? _resourceMan;
    private static CultureInfo _resourceCulture = CultureInfo.CurrentUICulture;
    
    private static System.Resources.ResourceManager ResourceManager {
        get {
            if (_resourceMan != null) return _resourceMan;
            lock (Lock)
            {
                _resourceMan ??= new System.Resources.ResourceManager("easpace.Assets.Localization.Resources",
                    typeof(LocalizationService).Assembly);
            }
            return _resourceMan;
        }
    }

    public static void ChangeLanguage(CultureInfo cultureInfo)
    {
        _resourceCulture = cultureInfo;
    }
    
    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, _resourceCulture) ?? "[UNDEFINED_LOCALIZATION_KEY]";
    }
    
    public string Key { get; set; } = string.Empty;
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return GetString(Key);
    }
}