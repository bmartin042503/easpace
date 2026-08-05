// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

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