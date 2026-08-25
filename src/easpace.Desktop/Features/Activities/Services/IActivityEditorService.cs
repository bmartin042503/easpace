// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.Contracts;
using easpace.Desktop.Features.Activities.ViewModels;

namespace easpace.Desktop.Features.Activities.Services;

internal interface IActivityEditorService
{
    UpdateActivityRequest GetUpdateRequest(ActivityViewModel activity);
}