// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Features.Activities.Constants;
using easpace.Desktop.Validation;

namespace easpace.Desktop.Features.Activities.Validation;

internal static class ActivityValidator
{
    public static IEnumerable<ValidationIssue> Validate(ActivityValidationInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            yield return new ValidationIssue(nameof(input.Name), "FormValidation.Name.Required");
        }
        else if (input.Name.Length < 3)
        {
            yield return new ValidationIssue(nameof(input.Name), "FormValidation.Name.MinLength");
        }
        else if (input.Name.Length > 64)
        {
            yield return new ValidationIssue(nameof(input.Name), "FormValidation.Name.MaxLength");
        }

        if (input.Type is not (ActivityType.Trend or ActivityType.Milestone)) yield break;

        if (input.Unit is { Length: > 16 })
        {
            yield return new ValidationIssue(nameof(input.Unit), "FormValidation.Unit.MaxLength");
        }

        if (input.Type == ActivityType.Trend && !input.IsTargetChecked) yield break;

        if (input.Target is { } target)
        {
            if (!(target > 0 && target <= 10_000_000))
            {
                yield return new ValidationIssue(nameof(input.Target), "FormValidation.Target.Range");
            }
        }
        else if (input.Type == ActivityType.Milestone)
        {
            yield return new ValidationIssue(nameof(input.Target), "FormValidation.Target.Required");
        }
    }
}
