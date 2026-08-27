// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using easpace.Desktop.Constants;
using easpace.Desktop.Data;
using easpace.Desktop.Factories;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;
using easpace.Desktop.Features.Activities.ViewModels;
using easpace.Desktop.Features.Journal.Services;
using easpace.Desktop.Features.Journal.ViewModels;
using easpace.Desktop.Features.Mood.Services;
using easpace.Desktop.Features.Mood.ViewModels;
using easpace.Desktop.Features.Wellness.Services;
using easpace.Desktop.Features.Wellness.ViewModels;
using easpace.Desktop.Security;
using easpace.Desktop.Services.Core;
using easpace.Desktop.Services.Data;
using easpace.Desktop.Services.Presentation;
using easpace.Desktop.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace easpace.Desktop.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public void AddCommonServices()
        {
            collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

            collection.AddSingleton<ApplicationService>();
            collection.AddSingleton<IApplicationService>(sp => sp.GetRequiredService<ApplicationService>());
            
            collection.AddSingleton<ToastMessageService>();
            collection.AddSingleton<IToastMessageService>(sp => sp.GetRequiredService<ToastMessageService>());
            
            collection.AddSingleton<DialogService>();
            collection.AddSingleton<IDialogService>(sp => sp.GetRequiredService<DialogService>());
            
            collection.AddSingleton<DataWipeService>();
            collection.AddSingleton<IDataWipeService>(sp => sp.GetRequiredService<DataWipeService>());

            collection.AddSingleton<PreferencesService>();
            collection.AddSingleton<IPreferencesService>(sp => sp.GetRequiredService<PreferencesService>());
            
            collection.AddTransient<UpdateService>();
            collection.AddTransient<IUpdateService>(sp => sp.GetRequiredService<UpdateService>());

            collection.AddTransient<WindowService>();
            collection.AddTransient<IWindowService>(sp => sp.GetRequiredService<WindowService>());

            collection.AddSingleton<ActivityService>();
            collection.AddSingleton<IActivityService>(sp => sp.GetRequiredService<ActivityService>());

            collection.AddSingleton<ActivityEditorService>();
            collection.AddSingleton<IActivityEditorService>(sp => sp.GetRequiredService<ActivityEditorService>());

            collection.AddSingleton<ActivityDataEntryService>();
            collection.AddSingleton<IActivityDataEntryService>(sp => sp.GetRequiredService<ActivityDataEntryService>());

            collection.AddSingleton<JournalEntryService>();
            collection.AddSingleton<IJournalEntryService>(sp => sp.GetRequiredService<JournalEntryService>());

            collection.AddSingleton<MoodEntryService>();
            collection.AddSingleton<IMoodEntryService>(sp => sp.GetRequiredService<MoodEntryService>());

            collection.AddSingleton<BreathingTechniqueService>();
            collection.AddSingleton<IBreathingTechniqueService>(sp =>
                sp.GetRequiredService<BreathingTechniqueService>());

            collection.AddSingleton<WellnessSessionEntryService>();
            collection.AddSingleton<IWellnessSessionEntryService>(sp =>
                sp.GetRequiredService<WellnessSessionEntryService>());

            collection.AddSingleton<TrendActivityDataProvider>();
            collection.AddSingleton<ITrendActivityDataProvider>(sp =>
                sp.GetRequiredService<TrendActivityDataProvider>());

            collection.AddSingleton<RoutineActivityDataProvider>();
            collection.AddSingleton<IRoutineActivityDataProvider>(sp =>
                sp.GetRequiredService<RoutineActivityDataProvider>());

            collection.AddSingleton<PageFactory>();

            collection.AddSingleton<MainViewModel>();

            collection.AddSingleton<OnboardingPageViewModel>();
            collection.AddSingleton<JournalPageViewModel>();
            collection.AddSingleton<ActivitiesPageViewModel>();
            collection.AddSingleton<MoodPageViewModel>();
            collection.AddSingleton<WellnessPageViewModel>();
            collection.AddSingleton<SettingsViewModel>();

            collection.AddSingleton<Func<ApplicationPage, PageViewModel>>(serviceProvider => page => page switch
            {
                ApplicationPage.Intro => serviceProvider.GetRequiredService<OnboardingPageViewModel>(),
                ApplicationPage.Journal => serviceProvider.GetRequiredService<JournalPageViewModel>(),
                ApplicationPage.Activities => serviceProvider.GetRequiredService<ActivitiesPageViewModel>(),
                ApplicationPage.Mood => serviceProvider.GetRequiredService<MoodPageViewModel>(),
                ApplicationPage.Wellness => serviceProvider.GetRequiredService<WellnessPageViewModel>(),
                ApplicationPage.Settings => serviceProvider.GetRequiredService<SettingsViewModel>(),
                _ => throw new InvalidOperationException()
            });
        }

        public void AddDatabaseServices()
        {
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "easpace");
            Directory.CreateDirectory(folderPath);
            var dbPath = Path.Combine(folderPath, "easpace.db");

            var password = SecureKeyManager.GetOrGenerateDbPassword();

            collection.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = $"Data Source={dbPath};Password={password};";
                options.UseSqlite(connectionString, o => o.MigrationsAssembly("easpace.Desktop"));
            });

            collection.AddTransient<DbSeeder>();
        }
    }
}