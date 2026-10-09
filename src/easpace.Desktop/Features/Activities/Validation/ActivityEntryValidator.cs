// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using easpace.Desktop.Validation;

namespace easpace.Desktop.Features.Activities.Validation;

internal static class ActivityEntryValidator
{
    public static IEnumerable<ValidationIssue> Validate(DateTime? selectedDate)
    {
        if (!selectedDate.HasValue)
        {
            yield return new ValidationIssue("SelectedDate", "FormValidation.Date.Required");
        }
    }

    public static IEnumerable<ValidationIssue> Validate(DateTime? selectedDate, double? numericValue)
    {
        foreach (var issue in Validate(selectedDate)) yield return issue;

        if (!numericValue.HasValue)
        {
            yield return new ValidationIssue("NumericValue", "FormValidation.NumericValue.Required");
        }
    }
}
