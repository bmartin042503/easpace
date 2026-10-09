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
    private static readonly Version Version = new(0, 2, 0);

    private readonly Dictionary<string, JsonElement> _preferences = new();
    private readonly Lock _lock = new();
    private readonly string _preferencesPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public PreferencesService()
    {
        var dirPath = AppStoragePaths.RoamingPath;
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        _preferencesPath = Path.Combine(dirPath, "preferences.json");
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
            SavePreference(PreferenceKey.Translucency, false);
        }
    }

    private void LoadPreferences()
    {
        lock (_lock)
        {
            if (!File.Exists(_preferencesPath)) return;

            Dictionary<string, JsonElement>? dict;

            try
            {
                var json = File.ReadAllText(_preferencesPath);
                dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                // an unreadable or corrupt file must not block startup, fall back to defaults
                // which overwrite the broken file on the next save
                _preferences.Clear();
                SetDefaultPreferences();
                return;
            }

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

        var wellnessShowTimerSetting = JsonSerializer.SerializeToElement(true);
        _preferences[PreferenceKey.WellnessShowTimer] = wellnessShowTimerSetting;

        var glassMorphismSetting = JsonSerializer.SerializeToElement(false);
        _preferences[PreferenceKey.Translucency] = glassMorphismSetting;

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

            // write to a temp file first, then swap it in, so an interrupted write can't leave a truncated file
            var tempPath = _preferencesPath + ".tmp";

            try
            {
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _preferencesPath, overwrite: true);
            }
            catch
            {
                File.Delete(tempPath);
                throw;
            }
        }
    }
}