using System;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Constants;
using easpace.Factories;
using easpace.Services;
using easpace.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace easpace.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public void AddCommonServices()
        {
            collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            
            collection.AddSingleton<PreferencesService>();
        
            collection.AddSingleton<PageFactory>();
        
            collection.AddSingleton<MainViewModel>();
        
            collection.AddSingleton<IntroViewModel>();
            collection.AddSingleton<JournalViewModel>();
            collection.AddSingleton<GrowthViewModel>();
            collection.AddSingleton<MoodViewModel>();
            collection.AddSingleton<WellnessViewModel>();
            collection.AddSingleton<SettingsViewModel>();
        
            collection.AddSingleton<Func<ApplicationPage, PageViewModel>>(serviceProvider => page => page switch
            {
                ApplicationPage.Intro => serviceProvider.GetRequiredService<IntroViewModel>(),
                ApplicationPage.Journal => serviceProvider.GetRequiredService<JournalViewModel>(),
                ApplicationPage.Growth => serviceProvider.GetRequiredService<GrowthViewModel>(),
                ApplicationPage.Mood => serviceProvider.GetRequiredService<MoodViewModel>(),
                ApplicationPage.Wellness => serviceProvider.GetRequiredService<WellnessViewModel>(),
                ApplicationPage.Settings => serviceProvider.GetRequiredService<SettingsViewModel>(),
                _ => throw new InvalidOperationException()
            });
        }
    }
}