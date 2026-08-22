// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;

namespace easpace.Desktop.Features.Activities.Services;

public interface IActivityService
{
    Task<Activity> CreateActivityAsync(CreateActivityRequest createRequest);
    Task<IReadOnlyList<Activity>> GetActivitiesAsync();
    Task<Activity?> UpdateActivityAsync(Guid activityId, UpdateActivityRequest updateRequest);
    Task<bool> DeleteActivityAsync(Guid activityId);
}