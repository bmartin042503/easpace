using Avalonia;
using System;
using System.Threading.Tasks;
using easpace.Extensions;
using Microsoft.Extensions.Hosting;

namespace easpace;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddCommonServices();
        
        using var host = builder.Build();
        
        await host.StartAsync();
        
        App.ConfigureServices(host.Services);
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        
        await host.StopAsync();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}