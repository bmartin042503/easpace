// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace easpace.Desktop.Models.Activities;

public partial class MilestoneActivity : NumericActivity
{
    [NotifyPropertyChangedFor(nameof(HasValidTargetDate))]
    [ObservableProperty] private DateTimeOffset? _targetDate;
    public double EntriesSum => Entries.Sum(entry => entry.Value);
    
    public bool HasValidTargetDate => TargetDate.HasValue && TargetDate.Value != DateTimeOffset.MinValue;

    protected override void OnCollectionChanged()
    {
        base.OnCollectionChanged();
        OnPropertyChanged(nameof(EntriesSum));
    }
}