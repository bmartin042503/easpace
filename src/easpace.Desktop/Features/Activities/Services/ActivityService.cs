// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;

namespace easpace.Desktop.Features.Activities.Services;

// Temporary in-memory implementation.
public class ActivityService : IActivityService
{
    private readonly IDataEntryService _dataEntryService;
    private readonly List<Activity> _activities = [];

    public ActivityService(IDataEntryService dataEntryService)
    {
        _dataEntryService = dataEntryService;
    }

    public Activity CreateActivity(CreateActivityRequest createRequest)
    {
        switch (createRequest.Type)
        {
            case ActivityType.Trend:

                var trendActivity = new TrendActivity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.Now,
                    Name = createRequest.Name,
                    Unit = createRequest.Unit,
                    Target = createRequest.Target,
                    IsArchived = false
                };

                _activities.Add(trendActivity);
                return trendActivity;

            case ActivityType.Milestone:

                var milestoneActivity = new MilestoneActivity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.Now,
                    Name = createRequest.Name,
                    Unit = createRequest.Unit,
                    Target = createRequest.Target,
                    TargetDate = createRequest.TargetDate,
                    IsArchived = false
                };

                _activities.Add(milestoneActivity);
                return milestoneActivity;

            case ActivityType.Routine:

                var routineActivity = new RoutineActivity
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.Now,
                    Name = createRequest.Name,
                    IsArchived = false
                };

                _activities.Add(routineActivity);
                return routineActivity;

            default:
                throw new NotSupportedException($"Undefined activity type: {createRequest.Type}");
        }
    }

    public IReadOnlyList<Activity> GetActivities()
    {
        var activities = _activities.OrderByDescending(activity => activity.CreatedAt).ToList();

        foreach (var activity in activities)
        {
            var dataEntries = _dataEntryService.GetDataEntries(activity.Id);
            
            if (dataEntries.Any())
            {
                activity.Entries = dataEntries.ToList();
            }
            
        }
        
        return activities;
    }

    public Activity? UpdateActivity(Guid activityId, UpdateActivityRequest updateRequest)
    {
        var activity = _activities.FirstOrDefault(activity => activity.Id == activityId);
        
        switch (activity)
        {
            case null:
                return null;
            
            case TrendActivity trendActivity:
                trendActivity.Name = updateRequest.Name;
                trendActivity.Unit = updateRequest.Unit;
                trendActivity.Target = updateRequest.Target;
                trendActivity.IsArchived = updateRequest.IsArchived;
                return trendActivity;
            
            case MilestoneActivity milestoneActivity:
                milestoneActivity.Name = updateRequest.Name;
                milestoneActivity.Unit = updateRequest.Unit;
                milestoneActivity.Target = updateRequest.Target;
                milestoneActivity.TargetDate = updateRequest.TargetDate;
                milestoneActivity.IsArchived = updateRequest.IsArchived;
                return milestoneActivity;
            
            case RoutineActivity routineActivity:
                routineActivity.Name = updateRequest.Name;
                routineActivity.IsArchived = updateRequest.IsArchived;
                return routineActivity;
            
            default:
                throw new NotSupportedException($"Undefined activity type: {activity.GetType().Name}");
        }
    }

    public bool DeleteActivity(Guid activityId)
    {
        var activity = _activities.FirstOrDefault(activity => activity.Id == activityId);
        
        if (activity is null) return false;

        // this is a mistake, to delete data entries first, as we are unsure that deletion is successful for activity
        // but for a temp in-memory service its totally fine
        foreach (var dataEntry in activity.Entries)
        {
            _dataEntryService.DeleteDataEntry(dataEntry.Id);
        }
        
        return _activities.Remove(activity);
    }
}