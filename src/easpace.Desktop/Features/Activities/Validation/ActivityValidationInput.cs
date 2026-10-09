// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using easpace.Desktop.Features.Activities.Constants;

namespace easpace.Desktop.Features.Activities.Validation;

internal sealed record ActivityValidationInput(
    string? Name,
    ActivityType Type,
    double? Target,
    string? Unit,
    bool IsTargetChecked
);
