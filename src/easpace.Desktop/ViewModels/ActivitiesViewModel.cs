using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.JavaScript;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models.Activities;
using easpace.Desktop.ViewModels.Activities;

namespace easpace.Desktop.ViewModels;

public partial class ActivitiesViewModel : PageViewModel
{
    [ObservableProperty] private ObservableCollection<ActivityViewModel> _activityViewModels = [];
    [ObservableProperty] private ActivityViewModel? _selectedActivityViewModel;

    public ActivitiesViewModel()
    {
        Page = ApplicationPage.Activities;
        LoadMockData();
    }

    [RelayCommand]
    public void SelectActivityViewModel(object parameter)
    {
        if (parameter is not ActivityViewModel activityViewModel) return;
        SelectedActivityViewModel = activityViewModel;
    }

    private void LoadMockData()
    {
        ActivityViewModels =
        [
            new MilestoneActivityViewModel
            {
                BaseActivity = new MilestoneActivity
                {
                    Title = "Elolvasott könyvek",
                    TargetValue = 1000,
                    Unit = "oldal",
                    Entries =
                    {
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-2), Value = 15 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-1), Value = 32 }
                    }
                }
            },

            new TrendActivityViewModel
            {
                BaseActivity = new TrendActivity
                {
                    Title = "Testsúly követés",
                    TargetValue = 75.0,
                    TargetDate = DateTime.Now,
                    Unit = "kg",
                    Entries =
                    {
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-35), Value = 89.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-31), Value = 88.2 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-27), Value = 87.0 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-24), Value = 86.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-20), Value = 85.2 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-16), Value = 84.8 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-13), Value = 83.8 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-9), Value = 82.9 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-5), Value = 82.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-3), Value = 81.9 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-1), Value = 81.2 }
                    }
                }
            },
            
            new RoutineActivityViewModel
            {
                BaseActivity = new RoutineActivity
                {
                    Title = "Napi Meditáció",
                    Entries =
                    {
                        new RoutineDataEntry { Date = DateTime.Now.AddDays(-2), State = RoutineState.Completed },
                        new RoutineDataEntry { Date = DateTime.Now.AddDays(-1), State = RoutineState.NotCompleted },
                        new RoutineDataEntry { Date = DateTime.Now, State = RoutineState.Completed }
                    }
                }
            }
        ];
    }
}