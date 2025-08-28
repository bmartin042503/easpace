using System;
using easpace.Constants;
using easpace.Factories;
using easpace.Services;
using easpace.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace easpace.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddSingleton<ConfigurationService>();
        
        collection.AddSingleton<PageFactory>();
        
        collection.AddSingleton<MainViewModel>();
        
        collection.AddSingleton<IntroViewModel>();
        collection.AddSingleton<JournalViewModel>();
        collection.AddSingleton<MoodViewModel>();
        collection.AddSingleton<MeditationViewModel>();
        collection.AddSingleton<SettingsViewModel>();
        
        collection.AddSingleton<Func<ApplicationPage, PageViewModel>>(serviceProvider => page => page switch
        {
            ApplicationPage.Intro => serviceProvider.GetRequiredService<IntroViewModel>(),
            ApplicationPage.Journal => serviceProvider.GetRequiredService<JournalViewModel>(),
            ApplicationPage.Mood => serviceProvider.GetRequiredService<MoodViewModel>(),
            ApplicationPage.Meditation => serviceProvider.GetRequiredService<MeditationViewModel>(),
            ApplicationPage.Settings => serviceProvider.GetRequiredService<SettingsViewModel>(),
            _ => throw new InvalidOperationException()
        });
    }
}