// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace easpace.Desktop.Validation;

internal sealed class ValidationState
{
    private ILookup<string, ValidationIssue> _errors =
        Enumerable.Empty<ValidationIssue>().ToLookup(issue => issue.MemberName);

    public bool HasErrors => _errors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable<ValidationIssue> GetErrors(string? propertyName)
    {
        // An empty member name represents form-level errors.
        return _errors[propertyName ?? string.Empty];
    }

    public void SetErrors(IEnumerable<ValidationIssue> issues)
    {
        var newErrors = issues.ToLookup(issue => issue.MemberName);
        var changedMembers = _errors.Select(group => group.Key)
            .Union(newErrors.Select(group => group.Key))
            .Where(member => !_errors[member].SequenceEqual(newErrors[member]))
            .ToArray();

        _errors = newErrors;

        foreach (var member in changedMembers)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(member));
        }
    }
}