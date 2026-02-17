using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace easpace.Services;

public interface IPreferencesService
{
    T ReadPreference<T>(string key, T defaultValue = default!);
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
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "easpace");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        _preferencesPath = Path.Combine(folder, "preferences.json");
        LoadPreferences();
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

    public T ReadPreference<T>(string key, T defaultValue = default!)
    {
        lock (_lock)
        {
            if (!_preferences.TryGetValue(key, out var element)) return defaultValue;
            try
            {
                return element.Deserialize<T>() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
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
