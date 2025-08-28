using System;
using System.Configuration;
using Avalonia.Styling;

namespace easpace.Services;

public static class ConfigurationKey
{
    public const string FirstTime = "first-time";
    public const string Language = "language";
    public const string ColorScheme = "color-scheme";
}

public interface IConfigurationService
{
    string ReadSetting(string key);
    void SaveSetting(string key, string value);
}

public class ConfigurationService : IConfigurationService
{
    public string ReadSetting(string key)
    {
        try
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings[key] ?? string.Empty;
        }
        catch (ConfigurationErrorsException e)
        {
            Console.WriteLine(e.Message);
        }
        return string.Empty;
    }

    public void SaveSetting(string key, string value)
    {
        try
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            if (Avalonia.Application.Current is not App app) return;
            if (key == ConfigurationKey.ColorScheme)
            {
                app.RequestedThemeVariant = value == "light" ? ThemeVariant.Light : ThemeVariant.Dark;
            }
        }
        catch (ConfigurationErrorsException e)
        {
            Console.WriteLine(e.Message);
        }
    }
}