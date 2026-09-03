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
using easpace.Desktop.Features.Activities.Entities.DataEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace easpace.Desktop.Features.Activities.Services;

internal class ActivityService : IActivityService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(AppDbContext dbContext, ILogger<ActivityService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Activity> CreateActivityAsync(CreateActivityRequest createRequest)
    {
        try
        {
            _logger.LogInformation("Creating new activity of type {Type} with name '{Name}'", createRequest.Type,
                createRequest.Name);

            Activity newActivity = createRequest.Type switch
            {
                ActivityType.Trend => new TrendActivity
                {
                    Name = createRequest.Name,
                    Unit = createRequest.Unit,
                    Target = createRequest.Target,
                    Aggregation = createRequest.Aggregation ?? TrendAggregation.Average
                },
                ActivityType.Milestone => new MilestoneActivity
                {
                    Name = createRequest.Name,
                    Unit = createRequest.Unit,
                    Target = createRequest.Target,
                    StartDate = createRequest.StartDate,
                    TargetDate = createRequest.TargetDate
                },
                ActivityType.Routine => new RoutineActivity { Name = createRequest.Name },
                _ => throw new NotSupportedException()
            };

            newActivity.CreatedAt = DateTimeOffset.Now;

            _dbContext.Activities.Add(newActivity);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Activity successfully created with ID {Id}", newActivity.Id);
            return newActivity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create activity of type {Type}", createRequest.Type);
            throw;
        }
    }

    public async Task<IReadOnlyList<Activity>> GetActivitiesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all activities from database");

            // don't use OrderByDescending on dbContext with the CreatedAt column
            // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses

            var activities = await _dbContext.Activities
                .Include(a => a.Entries)
                .AsNoTracking()
                .ToListAsync();

            return activities.OrderByDescending(a => a.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch activities from database");
            throw;
        }
    }

    public async Task<Activity?> UpdateActivityAsync(Guid activityId, UpdateActivityRequest updateRequest)
    {
        try
        {
            var activity = await _dbContext.Activities.FindAsync(activityId);

            if (activity == null)
            {
                _logger.LogWarning("Attempted to update non-existent activity with ID {Id}", activityId);
                return null;
            }

            switch (activity)
            {
                case TrendActivity trendActivity:
                    trendActivity.Name = updateRequest.Name;
                    trendActivity.Unit = updateRequest.Unit;
                    trendActivity.Target = updateRequest.Target;
                    trendActivity.Aggregation = updateRequest.Aggregation ?? TrendAggregation.Average;
                    activity = trendActivity;
                    break;

                case MilestoneActivity milestoneActivity:
                    milestoneActivity.Name = updateRequest.Name;
                    milestoneActivity.Unit = updateRequest.Unit;
                    milestoneActivity.Target = updateRequest.Target;
                    milestoneActivity.StartDate = updateRequest.StartDate;
                    milestoneActivity.TargetDate = updateRequest.TargetDate;

                    activity = milestoneActivity;
                    break;

                case RoutineActivity routineActivity:
                    routineActivity.Name = updateRequest.Name;
                    activity = routineActivity;
                    break;
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Activity with ID {Id} successfully updated", activityId);
            return activity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update activity with ID {Id}", activityId);
            throw;
        }
    }

    public async Task<Activity?> ToggleArchiveAsync(Guid activityId)
    {
        try
        {
            var activity = await _dbContext.Activities.FindAsync(activityId);
            if (activity == null)
            {
                _logger.LogWarning("Attempted to toggle archive for non-existent activity with ID {Id}", activityId);
                return null;
            }

            activity.IsArchived = !activity.IsArchived;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Activity {Id} archive status changed to {IsArchived}", activityId,
                activity.IsArchived);
            return activity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle archive status for activity with ID {Id}", activityId);
            throw;
        }
    }

    public async Task<bool> DeleteActivityAsync(Guid activityId)
    {
        try
        {
            var activity = await _dbContext.Activities.FindAsync(activityId);

            if (activity is null)
            {
                _logger.LogWarning("Attempted to delete non-existent activity with ID {Id}", activityId);
                return false;
            }

            _dbContext.Activities.Remove(activity);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Activity with ID {Id} successfully deleted", activityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete activity with ID {Id}", activityId);
            throw;
        }
    }
}