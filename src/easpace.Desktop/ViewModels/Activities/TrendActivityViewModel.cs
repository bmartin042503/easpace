using System;
using System.Linq;
using easpace.Desktop.Models.Activities;

namespace easpace.Desktop.ViewModels.Activities;

public class TrendActivityViewModel : ActivityViewModel
{
    public TrendActivity Activity => (TrendActivity)BaseActivity;
    
    public TrendActivityViewModel(TrendActivity activity)
    {
        BaseActivity = activity;
    }

    public TrendActivityViewModel()
    {
    }
}