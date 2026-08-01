using System.Linq;

namespace easpace.Desktop.Models.Activities;

public class MilestoneActivity : NumericActivity
{
    public double EntriesSum => Entries.Sum(entry => entry.Value);

    protected override void OnCollectionChanged()
    {
        base.OnCollectionChanged();
        OnPropertyChanged(nameof(EntriesSum));
    }
}