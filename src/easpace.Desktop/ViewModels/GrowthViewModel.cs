using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Constants.Keys;
using easpace.Desktop.Models.Growth;

namespace easpace.Desktop.ViewModels;

public partial class GrowthViewModel : PageViewModel
{
    [ObservableProperty] private ObservableCollection<GrowthActivity> _activities = [];
    [ObservableProperty] private GrowthActivity? _selectedActivity;
    
    public GrowthViewModel()
    {
        Page = ApplicationPage.Growth;
        LoadMockData();
    }

    [RelayCommand]
    public void SelectActivity(object parameter)
    {
        if (parameter is not GrowthActivity activity) return;
        SelectedActivity = activity;
    }
    
    private void LoadMockData()
    {
        Activities =
        [
            new MilestoneActivity
            {
                Title = "Elolvasott könyvek",
                TargetValue = 1000,
                Unit = "oldal",
                Entries =
                {
                    new NumericDataEntry { Date = DateTime.Now.AddDays(-2), Value = 15 },
                    new NumericDataEntry { Date = DateTime.Now.AddDays(-1), Value = 32 }
                }
            },

            new TrendActivity
            {
                Title = "Testsúly követés",
                TargetValue = 75.0,
                Unit = "kg",
                Entries =
                {
                    new NumericDataEntry { Date = DateTime.Now.AddDays(-5), Value = 82.5 },
                    new NumericDataEntry { Date = DateTime.Now.AddDays(-1), Value = 81.2 }
                }
            },

            new RoutineActivity
            {
                Title = "Napi Meditáció",
                Entries =
                {
                    new RoutineDataEntry { Date = DateTime.Now.AddDays(-2), State = RoutineState.Completed },
                    new RoutineDataEntry { Date = DateTime.Now.AddDays(-1), State = RoutineState.NotCompleted },
                    new RoutineDataEntry { Date = DateTime.Now, State = RoutineState.Completed }
                }
            }
        ];
    }
}
