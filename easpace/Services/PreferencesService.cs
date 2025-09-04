using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace easpace.Services;

public static class PreferenceKey
{
    public const string NewUser = "new-user";
    public const string Language = "language";
    public const string ColorScheme = "color-scheme";
}

public interface IPreferencesService
{
    T ReadPreference<T>(string key);
    void SavePreference<T>(string key, T value);
}

public class PreferencesService : IPreferencesService
{
    private readonly Dictionary<string, JsonElement> _preferences = new();
    private readonly Lock _lock = new();
    private readonly string _preferencesPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public PreferencesService()
    {
        _preferencesPath = GetPreferencesPath("easpace");
        LoadPreferences();
    }

    private static string GetPreferencesPath(string appName)
    {
        string folder;
        if (OperatingSystem.IsWindows())
        {
            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", appName);
        }
        else
        {
            folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", appName);
        }

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        
        return Path.Combine(folder, "user-preferences.json");
    }

    private void LoadPreferences()
    {
        lock (_lock)
        {
            if (!File.Exists(_preferencesPath)) return;
            var json = File.ReadAllText(_preferencesPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict == null) return;
            _preferences.Clear();
            foreach (var kvp in dict)
                _preferences[kvp.Key] = kvp.Value;
        }
    }

    private void SavePreferences()
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(_preferences, _jsonOptions);
            File.WriteAllText(_preferencesPath, json);
        }
    }

    public T ReadPreference<T>(string key)
    {
        lock (_lock)
        {
            if (!_preferences.TryGetValue(key, out var element)) return default!;
            try
            {
                return element.Deserialize<T>()!;
            }
            catch
            {
                return default!;
            }
        }
    }

    public void SavePreference<T>(string key, T value)
    {
        lock (_lock)
        {
            var element = JsonSerializer.SerializeToElement(value);
            _preferences[key] = element;
            SavePreferences();
        }
    }
}