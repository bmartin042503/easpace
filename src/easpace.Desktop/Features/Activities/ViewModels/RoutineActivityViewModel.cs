// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using easpace.Desktop.Features.Activities.Services;
using easpace.Desktop.Features.Activities.Services.DataProviders;

namespace easpace.Desktop.Features.Activities.ViewModels;

public partial class RoutineActivityViewModel : ActivityViewModel
{
    private readonly List<RoutineDataEntry> _routineDataEntries;
    private readonly IRoutineActivityDataProvider _routineActivityDataProvider;
    private readonly IActivityService _activityService;

    public AvaloniaList<RoutineMonth> RoutineMonths { get; } = [];

    public RoutineActivityViewModel(
        RoutineActivity routineActivity,
        IRoutineActivityDataProvider routineActivityDataProvider,
        IDataEntryService dataEntryService,
        IActivityService activityService) : base(routineActivity, dataEntryService)
    {
        _routineActivityDataProvider = routineActivityDataProvider;
        _routineDataEntries = routineActivity.Entries.OfType<RoutineDataEntry>().ToList();
        _activityService = activityService;
        
        LoadRoutineMonths();
    }
    
    public override Activity? UpdateFrom(UpdateActivityRequest updateRequest)
    {
        var updated = _activityService.UpdateActivity(Id, updateRequest);

        if (updated is null) return null;
        
        Name = updated.Name;
        
        return updated;
    }

    private void LoadRoutineMonths()
    {
        var months = _routineActivityDataProvider.GetRoutineMonths(_routineDataEntries);

        RoutineMonths.Clear();
        RoutineMonths.AddRange(months);
    }
}