// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.ViewModels;

namespace easpace.Desktop.Features.Activities.Services;

public class ActivityEditorService : IActivityEditorService
{
    public UpdateActivityRequest GetUpdateRequest(ActivityViewModel activity)
    {
        switch (activity)
        {
            case TrendActivityViewModel trendActivity:

                return new UpdateActivityRequest(
                    Name: trendActivity.Name,
                    Target: trendActivity.Target,
                    Unit: trendActivity.Unit,
                    TargetDate: null,
                    IsArchived: trendActivity.IsArchived
                );

            case MilestoneActivityViewModel milestoneActivity:
                
                return new UpdateActivityRequest(
                    Name: milestoneActivity.Name,
                    Target: milestoneActivity.Target,
                    Unit: milestoneActivity.Unit,
                    TargetDate: milestoneActivity.TargetDate,
                    IsArchived: milestoneActivity.IsArchived
                );

            case RoutineActivityViewModel routineActivity:
                
                return new UpdateActivityRequest(
                    Name: routineActivity.Name,
                    null,
                    null,
                    null,
                    IsArchived: routineActivity.IsArchived
                );
            
            default:
                throw new NotSupportedException($"Undefined activity type: {activity.GetType().Name}");
        }
    }
}