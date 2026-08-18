// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using easpace.Desktop.Features.Journal.ViewModels;
using easpace.Desktop.Features.Journal.Views;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Activities;
using easpace.Desktop.ViewModels.Dialogs;
using easpace.Desktop.Views;
using easpace.Desktop.Views.Activities;
using easpace.Desktop.Views.Dialogs;

namespace easpace.Desktop.DataTemplates;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        return param switch
        {
            // Pages
            IntroViewModel vm => CreateView(new IntroView(), vm),
            JournalPageViewModel vm => CreateView(new JournalPageView(), vm),
            ActivitiesViewModel vm => CreateView(new ActivitiesView(), vm),
            MoodViewModel vm => CreateView(new MoodView(), vm),
            WellnessViewModel vm => CreateView(new WellnessView(), vm),
            SettingsViewModel vm => CreateView(new SettingsView(), vm),
            
            // Activities
            TrendActivityViewModel vm => CreateView(new TrendActivityView(), vm),
            MilestoneActivityViewModel vm => CreateView(new MilestoneActivityView(), vm),
            RoutineActivityViewModel vm => CreateView(new RoutineActivityView(), vm),
            
            // Activity editor
            ActivityEditorViewModel vm => CreateView(new ActivityEditorView(), vm),
            
            // Dialogs
            ConfirmDialogViewModel vm => CreateView(new ConfirmDialogView(), vm),
            NumericEntryDialogViewModel vm => CreateView(new NumericEntryDialogView(), vm),
            RoutineEntryDialogViewModel vm => CreateView(new RoutineEntryDialogView(), vm),
            
            // Wellness
            WellnessConfigurationViewModel vm => CreateView(new WellnessConfigurationView(), vm),
            WellnessSessionViewModel vm => CreateView(new WellnessSessionView(), vm),
            WellnessEndingViewModel vm => CreateView(new WellnessEndingView(), vm),
            
            _ => null
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase or ValidatorViewModelBase;
    }

    private static Control CreateView(Control view, object viewModel)
    {
        view.DataContext = viewModel;
        return view;
    }
}