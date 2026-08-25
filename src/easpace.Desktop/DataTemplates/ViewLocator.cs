// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using easpace.Desktop.Features.Activities.ViewModels;
using easpace.Desktop.Features.Activities.ViewModels.Dialogs;
using easpace.Desktop.Features.Activities.Views;
using easpace.Desktop.Features.Activities.Views.Dialogs;
using easpace.Desktop.Features.Journal.ViewModels;
using easpace.Desktop.Features.Journal.Views;
using easpace.Desktop.Features.Mood.ViewModels;
using easpace.Desktop.Features.Mood.Views;
using easpace.Desktop.Features.Wellness.ViewModels;
using easpace.Desktop.Features.Wellness.Views;
using easpace.Desktop.ViewModels;
using easpace.Desktop.ViewModels.Dialogs;
using easpace.Desktop.Views;
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
            ActivitiesPageViewModel vm => CreateView(new ActivitiesPageView(), vm),
            MoodPageViewModel vm => CreateView(new MoodPageView(), vm),
            WellnessPageViewModel vm => CreateView(new WellnessPageView(), vm),
            SettingsViewModel vm => CreateView(new SettingsView(), vm),
            
            // Activities
            TrendActivityViewModel vm => CreateView(new TrendActivityView(), vm),
            MilestoneActivityViewModel vm => CreateView(new MilestoneActivityView(), vm),
            RoutineActivityViewModel vm => CreateView(new RoutineActivityView(), vm),
            
            // Activity editor
            ActivityEditorViewModel vm => CreateView(new ActivityEditorView(), vm),
            
            // Dialogs (make sure dialogs are set in the correct order)
            LegalInfoDialogViewModel vm => CreateView(new LegalInfoDialogView(), vm),
            ConfirmDialogViewModel vm => CreateView(new ConfirmDialogView(), vm),
            ErrorDialogViewModel vm => CreateView(new ErrorDialogView(), vm),
            InfoDialogViewModel vm => CreateView(new InfoDialogView(), vm),
            NumericEntryDialogViewModel vm => CreateView(new NumericEntryDialogView(), vm),
            RoutineEntryDialogViewModel vm => CreateView(new RoutineEntryDialogView(), vm),
            
            // Wellness
            WellnessStartViewModel vm => CreateView(new WellnessStartView(), vm),
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