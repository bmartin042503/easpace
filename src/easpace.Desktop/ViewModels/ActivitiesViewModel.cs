using System;
using System.Collections.ObjectModel;
using System.Linq;
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

        if (ActivityViewModels.Any())
        {
            SelectedActivityViewModel = ActivityViewModels.First();
        }
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
                    Title = "Testsúly",
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

            new TrendActivityViewModel
            {
                BaseActivity = new TrendActivity
                {
                    Title = "Testzsír százalék",
                    TargetValue = 15.0,
                    TargetDate = DateTime.Now,
                    Unit = "%",
                    Entries =
                    {
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-90), Value = 24.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-87), Value = 24.6 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-84), Value = 24.2 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-81), Value = 24.0 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-78), Value = 23.8 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-75), Value = 23.9 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-72), Value = 23.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-69), Value = 23.1 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-66), Value = 23.2 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-63), Value = 22.8 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-60), Value = 22.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-57), Value = 22.6 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-54), Value = 22.1 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-51), Value = 21.8 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-48), Value = 21.9 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-45), Value = 21.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-42), Value = 21.1 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-39), Value = 20.8 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-36), Value = 20.9 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-33), Value = 20.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-30), Value = 20.2 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-27), Value = 20.0 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-24), Value = 20.1 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-21), Value = 19.8 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-18), Value = 19.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-15), Value = 19.3 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-12), Value = 19.4 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-9), Value = 19.0 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-6), Value = 18.7 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-3), Value = 18.5 },
                        new NumericDataEntry { Date = DateTime.Now.AddDays(-1), Value = 18.2 }
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
            },
            
            GenerateMassiveTestData()
        ];
    }

    public static TrendActivityViewModel GenerateMassiveTestData(int dataPoints = 3000)
    {
        var viewModel = new TrendActivityViewModel
        {
            BaseActivity = new TrendActivity
            {
                Title = "Napi Lépésszám (Stresszteszt)",
                TargetValue = 10000.0,
                TargetDate = DateTime.Now,
                Unit = "lépés"
                // Az Entries listát a ciklusban töltjük fel
            }
        };

        // Fix seed-et használunk, hogy minden futtatáskor ugyanazt a görbét kapd (könnyebb tesztelni)
        var random = new Random(42);
        var startDate = DateTime.Now.AddDays(-dataPoints);

        // Kezdőérték
        double currentValue = 6000;

        for (int i = 0; i < dataPoints; i++)
        {
            // Valósághű ugrálások (néha sok, néha kevés lépés), enyhe pozitív eltolással
            currentValue += random.Next(-1500, 1550);

            // Limitáljuk az értékeket, hogy reális tartományban maradjanak (1000 és 25000 lépés között)
            if (currentValue < 1000) currentValue = 1000 + random.Next(0, 500);
            if (currentValue > 25000) currentValue = 25000 - random.Next(0, 500);

            viewModel.Activity.Entries.Add(new NumericDataEntry
            {
                Date = startDate.AddDays(i),
                Value = Math.Round(currentValue)
            });
        }

        return viewModel;
    }
}