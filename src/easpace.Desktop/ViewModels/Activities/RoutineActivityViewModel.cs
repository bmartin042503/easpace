using System;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public class RoutineActivityViewModel : ActivityViewModel
{
    public RoutineActivity Activity => (RoutineActivity)BaseActivity;
    
    public RoutineActivityViewModel(RoutineActivity activity)
    {
        BaseActivity = activity;
    }
    
    // empty constructor for AXAML preview
    public RoutineActivityViewModel() {}
}