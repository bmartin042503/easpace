// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using easpace.Desktop.Data;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;
using Microsoft.EntityFrameworkCore;

namespace easpace.Desktop.Features.Activities.Services;

public class ActivityService : IActivityService
{
    private readonly AppDbContext _dbContext;

    public ActivityService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Activity> CreateActivityAsync(CreateActivityRequest createRequest)
    {
        Activity newActivity = createRequest.Type switch
        {
            ActivityType.Trend => new TrendActivity
            {
                Name = createRequest.Name, Unit = createRequest.Unit, Target = createRequest.Target
            },
            ActivityType.Milestone => new MilestoneActivity
            {
                Name = createRequest.Name, Unit = createRequest.Unit, Target = createRequest.Target, TargetDate = createRequest.TargetDate
            },
            ActivityType.Routine => new RoutineActivity { Name = createRequest.Name },
            _ => throw new NotSupportedException()
        };

        _dbContext.Activities.Add(newActivity);
        await _dbContext.SaveChangesAsync();
        return newActivity;
    }

    public async Task<IReadOnlyList<Activity>> GetActivitiesAsync()
    {
        // don't use OrderByDescending on dbContext with the CreatedAt column
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses
        
        var activities = await _dbContext.Activities
            .Include(a => a.Entries)
            .AsNoTracking()
            .ToListAsync();

        return activities.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<Activity?> UpdateActivityAsync(Guid activityId, UpdateActivityRequest updateRequest)
    {
        var activity = await _dbContext.Activities.FindAsync(activityId);
        
        switch (activity)
        {
            case null:
                return null;
            
            case TrendActivity trendActivity:
                trendActivity.Name = updateRequest.Name;
                trendActivity.Unit = updateRequest.Unit;
                trendActivity.Target = updateRequest.Target;
                return trendActivity;
            
            case MilestoneActivity milestoneActivity:
                milestoneActivity.Name = updateRequest.Name;
                milestoneActivity.Unit = updateRequest.Unit;
                milestoneActivity.Target = updateRequest.Target;
                milestoneActivity.TargetDate = updateRequest.TargetDate;
                return milestoneActivity;
            
            case RoutineActivity routineActivity:
                routineActivity.Name = updateRequest.Name;
                return routineActivity;
        }
        
        await _dbContext.SaveChangesAsync();
        return activity;
    }

    public async Task<Activity?> ToggleArchiveAsync(Guid activityId)
    {
        var activity = await _dbContext.Activities.FindAsync(activityId);
        if (activity == null) return null;
        
        activity.IsArchived = !activity.IsArchived;
        await _dbContext.SaveChangesAsync();
        return activity;
    }

    public async Task<bool> DeleteActivityAsync(Guid activityId)
    {
        var activity = await _dbContext.Activities.FindAsync(activityId);
        if (activity is null) return false;
        
        _dbContext.Activities.Remove(activity);
        
        await _dbContext.SaveChangesAsync();
        return true;
    }
}