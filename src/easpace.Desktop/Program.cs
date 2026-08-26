// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using System;
using System.Globalization;
using System.Threading.Tasks;
using easpace.Desktop.Constants;
using easpace.Desktop.Data;
using easpace.Desktop.Extensions;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace easpace.Desktop;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddCommonServices();
        builder.Services.AddDatabaseServices();

        using var host = builder.Build();

        await host.StartAsync();

        using (var scope = host.Services.CreateScope())
        {
            // set language based on preferences, if it's empty save the current one
            // invariant is the default (English)
            
            var preferences = scope.ServiceProvider.GetRequiredService<IPreferencesService>();
            var languageSetting = preferences.ReadPreference<string>(PreferenceKey.Language);

            if (string.IsNullOrWhiteSpace(languageSetting))
            {
                var currentCulture = CultureInfo.CurrentCulture;
                
                switch (currentCulture.TwoLetterISOLanguageName)
                {
                    case "hu":
                        preferences.SavePreference(PreferenceKey.Language, "hu");
                        break;
                    
                    default:
                        preferences.SavePreference(PreferenceKey.Language, "en");
                        break;
                }
            }
            else
            {
                var cultureInfo = languageSetting switch
                {
                    "en" => CultureInfo.InvariantCulture,
                    "hu" => CultureInfo.GetCultureInfo("hu-HU"),
                    _ => CultureInfo.InvariantCulture
                };
                
                LocalizationService.ChangeLanguage(cultureInfo);
            }

            // migrate db if there are changes
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();

            // seed the db with default values
            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
        }

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
            .LogToTrace()
            .WithDataAnnotationsValidation();
}