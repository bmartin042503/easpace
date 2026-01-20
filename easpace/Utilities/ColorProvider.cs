using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using easpace.Constants;

namespace easpace.Utilities;

public static class ColorProvider
{
    public static IBrush GetColor(ApplicationColor applicationColor)
    {
        var defaultColorBrush = new ImmutableSolidColorBrush(Colors.Black);
        
        object? color = defaultColorBrush;
        var themes = Application.Current?.Resources.ThemeDictionaries;
        if (themes is null) return defaultColorBrush;

        themes.TryGetValue(
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out var theme
        );
        
        theme?.TryGetResource(
            applicationColor.ToString(),
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out color
        );

        return color switch
        {
            Color extractedColor => new ImmutableSolidColorBrush(extractedColor),
            LinearGradientBrush linearGradientBrush => linearGradientBrush,
            _ => color as ImmutableSolidColorBrush ?? defaultColorBrush
        };
    }
}