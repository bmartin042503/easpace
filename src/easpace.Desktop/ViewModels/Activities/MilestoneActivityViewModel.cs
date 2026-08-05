// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public class MilestoneActivityViewModel : ActivityViewModel
{
    public MilestoneActivity Activity => (MilestoneActivity)BaseActivity;
    
    public MilestoneActivityViewModel(MilestoneActivity activity)
    {
        BaseActivity = activity;
    }
    
    // empty constructor for AXAML preview
    public MilestoneActivityViewModel() {}
    
}