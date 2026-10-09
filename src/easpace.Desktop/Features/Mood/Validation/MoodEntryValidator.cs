// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using easpace.Desktop.Validation;

namespace easpace.Desktop.Features.Mood.Validation;

internal static class MoodEntryValidator
{
    public static IEnumerable<ValidationIssue> Validate(double value)
    {
        if (value is < 0 or > 1)
        {
            yield return new ValidationIssue("MoodSliderValue", "Mood.Validation.Range");
        }
    }
}
