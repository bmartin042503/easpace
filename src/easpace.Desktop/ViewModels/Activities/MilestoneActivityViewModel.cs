// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Models.Activities;
using easpace.Desktop.Services;

namespace easpace.Desktop.ViewModels.Activities;

public class MilestoneActivityViewModel : NumericActivityViewModel<MilestoneActivity>
{
    public MilestoneActivityViewModel(MilestoneActivity activity, IDialogService dialogService) : base(activity,
        dialogService)
    {
    }

    public override void RefreshDataEntries()
    {
        base.RefreshDataEntries();
        Activity.RefreshEntriesSum();
    }
}