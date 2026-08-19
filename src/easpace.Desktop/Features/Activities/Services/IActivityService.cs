// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.Entities;

namespace easpace.Desktop.Features.Activities.Services;

public interface IActivityService
{
    Activity CreateActivity(CreateActivityRequest createRequest);
    IReadOnlyList<Activity> GetActivities();
    Activity? UpdateActivity(Guid activityId, UpdateActivityRequest updateRequest);
    bool DeleteActivity(Guid activityId);
}