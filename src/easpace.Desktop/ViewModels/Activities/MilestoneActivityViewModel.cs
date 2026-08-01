using System;
using System.Linq;
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