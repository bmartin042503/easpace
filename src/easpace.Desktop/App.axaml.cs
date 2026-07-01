using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;
using easpace.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;

namespace easpace.Desktop;

public partial class App : Application
{
    public const string Version = "0.1.0";
    private static IServiceProvider? _services;

    public static void ConfigureServices(IServiceProvider services)
    {
        _services = services;
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (Avalonia.Controls.Design.IsDesignMode)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }
        
        if (_services == null)
        {
            throw new InvalidOperationException("Services are not initialized.");
        }
        
        RequestedThemeVariant = ThemeVariant.Light;
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
