using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using easpace.Constants.Themes;

namespace easpace.Utilities;

public static class ThemeProvider
{
    public static Color GetColor(ColorKey colorKey)
    {
        var resource = GetRawResource(colorKey);

        return resource switch
        {
            Color extractedColor => extractedColor,
            ISolidColorBrush brush => brush.Color,
            _ => Colors.Black
        };
    }

    public static IBrush GetBrush(BrushKey brushKey)
    {
        var resource = GetRawResource(brushKey);
        var defaultBrush = new ImmutableSolidColorBrush(Colors.Black);

        return resource switch
        {
            IBrush brush => brush,
            Color extractedColor => new ImmutableSolidColorBrush(extractedColor), 
            _ => defaultBrush
        };
    }
    
    public static DropShadowEffect? GetEffect(EffectKey effectKey)
    {
        return GetRawResource(effectKey) as DropShadowEffect;
    }
    
    private static object? GetRawResource<T>(T resourceKey) where T : Enum
    {
        object? resource = null;
        var themes = Application.Current?.Resources.ThemeDictionaries;
        if (themes is null) return null;

        themes.TryGetValue(
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out var theme
        );

        theme?.TryGetResource(
            resourceKey.ToString(),
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out resource
        );

        return resource;
    }
}
