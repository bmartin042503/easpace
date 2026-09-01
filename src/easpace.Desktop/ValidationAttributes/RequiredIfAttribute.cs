// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel.DataAnnotations;

namespace easpace.Desktop.ValidationAttributes;

internal class RequiredIfAttribute(string otherPropertyName, object desiredValue) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var instance = validationContext.ObjectInstance;
        var type = instance.GetType();
        
        var otherProperty = type.GetProperty(otherPropertyName);

        if (otherProperty == null)
        {
            return new ValidationResult($"{otherPropertyName} property cannot be found.");
        }
        
        var otherPropertyValue = otherProperty.GetValue(instance);

        if (Equals(otherPropertyValue, desiredValue))
        {
            if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
            {
                return new ValidationResult(ErrorMessage ?? "This field is required.");
            }
        }
        
        return ValidationResult.Success;
    }
}