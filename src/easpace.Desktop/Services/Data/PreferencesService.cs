// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using easpace.Desktop.Constants;
using easpace.Desktop.Services.Presentation;

namespace easpace.Desktop.Services.Data;

internal interface IPreferencesService
{
    T ReadPreference<T>(string key, T defaultValue = default!);
    void SavePreference<T>(string key, T value);
}

internal class PreferencesService : IPreferencesService
{
    private static readonly Version Version = new(0,2,0);
    
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
        
        var savedPreferencesVersion = ReadPreference<Version>(PreferenceKey.PreferencesVersion);
        if (savedPreferencesVersion < Version)
        {
            MigratePreferences();
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
    
    private void MigratePreferences()
    {
        // v0.1.0 -> v0.2.0
        
        var colorSchemeValue = ReadPreference<string>(PreferenceKey.ColorScheme);
        if (!string.IsNullOrEmpty(colorSchemeValue) 
            || colorSchemeValue == "system" || colorSchemeValue == "light" || colorSchemeValue == "dark")
        {
            var appearance = colorSchemeValue switch
            {
                "system" => ColorSchemeAppearance.Default,
                "light" => ColorSchemeAppearance.Light,
                "dark" => ColorSchemeAppearance.Dark,
                _ => ColorSchemeAppearance.Default
            };
            
            SavePreference(PreferenceKey.ColorSchemeAppearance, appearance);
            SavePreference(PreferenceKey.ColorScheme, ColorScheme.HavenBlue);
            SavePreference(PreferenceKey.PreferencesVersion, Version);
            SavePreference(PreferenceKey.Glassmorphism, false);
        }
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

    private void SetDefaultPreferences()
    {
        var colorScheme = JsonSerializer.SerializeToElement(ColorScheme.HavenBlue);
        _preferences[PreferenceKey.ColorScheme] = colorScheme;
        
        var colorSchemeAppearance = JsonSerializer.SerializeToElement(ColorSchemeAppearance.Default);
        _preferences[PreferenceKey.ColorSchemeAppearance] = colorSchemeAppearance;
        
        var wellnessFullScreenSetting = JsonSerializer.SerializeToElement(true);
        _preferences[PreferenceKey.WellnessFullScreen] = wellnessFullScreenSetting;
        
        var wellnessAnimatedBgSetting = JsonSerializer.SerializeToElement(true);
        _preferences[PreferenceKey.WellnessAnimatedBackground] = wellnessAnimatedBgSetting;
        
        var glassMorphismSetting = JsonSerializer.SerializeToElement(false);
        _preferences[PreferenceKey.Glassmorphism] = glassMorphismSetting;
        
        var version = JsonSerializer.SerializeToElement(Version);
        _preferences[PreferenceKey.PreferencesVersion] = version;
    }

    private void SavePreferences()
    {
        lock (_lock)
        {
            if (!File.Exists(_preferencesPath))
            {
                SetDefaultPreferences();
            }
            
            var json = JsonSerializer.Serialize(_preferences, _jsonOptions);
            File.WriteAllText(_preferencesPath, json);
        }
    }
}