// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia.Controls;
using Avalonia.Controls.Templates;
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
            IntroViewModel vm => CreateView(new IntroView(), vm),
            JournalViewModel vm => CreateView(new JournalView(), vm),
            ActivitiesViewModel vm => CreateView(new ActivitiesView(), vm),
            MoodViewModel vm => CreateView(new MoodView(), vm),
            WellnessViewModel vm => CreateView(new WellnessView(), vm),
            SettingsViewModel vm => CreateView(new SettingsView(), vm),
            ConfirmDialogViewModel vm => CreateView(new ConfirmDialogView(), vm),
            NumericEntryDialogViewModel vm => CreateView(new NumericEntryDialogView(), vm),
            RoutineEntryDialogViewModel vm => CreateView(new RoutineEntryDialogView(), vm),
            _ => null
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }

    private static Control CreateView(Control view, object viewModel)
    {
        view.DataContext = viewModel;
        return view;
    }
}