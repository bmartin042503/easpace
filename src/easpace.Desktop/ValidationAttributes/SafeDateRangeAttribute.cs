// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.ComponentModel.DataAnnotations;

namespace easpace.Desktop.ValidationAttributes;

internal class SafeDateRangeAttribute : ValidationAttribute
{
    private readonly int _minYear;
    private readonly int _maxYear;

    public SafeDateRangeAttribute(int minYear = 1900, int maxYear = 2100)
    {
        _minYear = minYear;
        _maxYear = maxYear;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }
        
        if (value is DateTime date)
        {
            if (date.Year < _minYear || date.Year > _maxYear)
            {
                return new ValidationResult(ErrorMessage ?? $"A dátum évének {_minYear} és {_maxYear} között kell lennie.");
            }
        }
        else if (value is DateTimeOffset dateOffset)
        {
            if (dateOffset.Year < _minYear || dateOffset.Year > _maxYear)
            {
                return new ValidationResult(ErrorMessage ?? $"A dátum évének {_minYear} és {_maxYear} között kell lennie.");
            }
        }
        else
        {
            return new ValidationResult("Invalid format. Value must be a valid date.");
        }
        
        return ValidationResult.Success;
    }
}