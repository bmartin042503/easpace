// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using easpace.Desktop.Validation;

namespace easpace.Desktop.ViewModels;

internal class ViewModelBase : ObservableObject, INotifyDataErrorInfo
{
    private readonly ValidationState _validation = new();

    public bool HasErrors => _validation.HasErrors;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public ViewModelBase()
    {
        _validation.ErrorsChanged += (_, args) => ErrorsChanged?.Invoke(this, args);
    }

    public IEnumerable GetErrors(string? propertyName) => _validation.GetErrors(propertyName);

    protected void SetValidationErrors(IEnumerable<ValidationIssue> issues)
    {
        var hadErrors = HasErrors;
        _validation.SetErrors(issues);

        if (hadErrors != HasErrors)
        {
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
