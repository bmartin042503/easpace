using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using easpace.Desktop.Constants;
using easpace.Desktop.Models;

namespace easpace.Desktop.ViewModels;

public partial class GrowthViewModel : PageViewModel
{
    public ObservableCollection<IGrowthTarget> GrowthTargets { get; set; }
    
    [ObservableProperty] private IGrowthTarget? _selectedGrowthTarget;

    public GrowthViewModel()
    {
        Page = ApplicationPage.Growth;

        // TODO: delete this after db is added
        var growthTargets = new List<IGrowthTarget>
        {
            new GrowthTarget<int>
            {
                Id = Guid.NewGuid(),
                Title = "Napi vízbevitel",
                Unit = "pohár",
                Goal = 8,
                Entries = new ObservableCollection<GrowthTargetEntry<int>>
                {
                    new() { Value = 2, Date = DateTime.Now.AddHours(-10) },
                    new() { Value = 5, Date = DateTime.Now.AddHours(-2) }
                }
            },
            
            new GrowthTarget<double>
            {
                Id = Guid.NewGuid(),
                Title = "Alvás időtartama",
                Unit = "óra",
                Goal = 7.5,
                Entries = new ObservableCollection<GrowthTargetEntry<double>>
                {
                    new() { Value = 6.2, Date = DateTime.Now.AddDays(-1) },
                    new() { Value = 8.0, Date = DateTime.Now }
                }
            },
            
            new GrowthTarget<int>
            {
                Id = Guid.NewGuid(),
                Title = "Meditáció",
                Unit = "perc",
                Goal = 20,
                Entries = new ObservableCollection<GrowthTargetEntry<int>>
                {
                    new() { Value = 10, Date = DateTime.Now.AddMinutes(-30) }
                }
            },
            
            new GrowthTarget<double>
            {
                Id = Guid.NewGuid(),
                Title = "Séta/Futás",
                Unit = "km",
                Goal = 5.0,
                Entries = new ObservableCollection<GrowthTargetEntry<double>>
                {
                    new() { Value = 1.2, Date = DateTime.Now.AddHours(-5) },
                    new() { Value = 3.8, Date = DateTime.Now.AddHours(-1) }
                }
            }
        };

        GrowthTargets = new ObservableCollection<IGrowthTarget>(growthTargets);
        SelectedGrowthTarget = GrowthTargets[0];
    }

    [RelayCommand]
    public void SelectTarget(object parameter)
    {
        if (parameter is not IGrowthTarget target) return;
        if (target.Id == SelectedGrowthTarget?.Id) return;
        SelectedGrowthTarget = target;
    }
}
