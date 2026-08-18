// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Factories;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Features.Journal.ViewModels;
using easpace.Desktop.Services;
using easpace.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace easpace.Desktop.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public void AddCommonServices()
        {
            collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            
            collection.AddSingleton<PreferencesService>();
            collection.AddSingleton<IPreferencesService>(sp => sp.GetRequiredService<PreferencesService>());
            
            collection.AddSingleton<DialogService>();
            collection.AddSingleton<IDialogService>(sp => sp.GetRequiredService<DialogService>());
            
            collection.AddSingleton<JournalService>();
            collection.AddSingleton<IJournalService>(sp => sp.GetRequiredService<JournalService>());
        
            collection.AddSingleton<PageFactory>();
        
            collection.AddSingleton<MainViewModel>();
        
            collection.AddSingleton<IntroViewModel>();
            collection.AddSingleton<JournalPageViewModel>();
            collection.AddSingleton<ActivitiesViewModel>();
            collection.AddSingleton<MoodViewModel>();
            collection.AddSingleton<WellnessViewModel>();
            collection.AddSingleton<SettingsViewModel>();
        
            collection.AddSingleton<Func<ApplicationPage, PageViewModel>>(serviceProvider => page => page switch
            {
                ApplicationPage.Intro => serviceProvider.GetRequiredService<IntroViewModel>(),
                ApplicationPage.Journal => serviceProvider.GetRequiredService<JournalPageViewModel>(),
                ApplicationPage.Activities => serviceProvider.GetRequiredService<ActivitiesViewModel>(),
                ApplicationPage.Mood => serviceProvider.GetRequiredService<MoodViewModel>(),
                ApplicationPage.Wellness => serviceProvider.GetRequiredService<WellnessViewModel>(),
                ApplicationPage.Settings => serviceProvider.GetRequiredService<SettingsViewModel>(),
                _ => throw new InvalidOperationException()
            });
        }
    }
}
