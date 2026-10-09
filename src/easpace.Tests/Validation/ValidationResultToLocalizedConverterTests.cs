// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Globalization;
using easpace.Desktop.Converters;
using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Validation;

public class ValidationResultToLocalizedConverterTests
{
    [Theory]
    [InlineData("en", "FormValidation.Name.Required", "Please enter a name.")]
    [InlineData("hu", "FormValidation.Name.Required", "Kérlek, adj meg egy nevet.")]
    [InlineData("en", "FormValidation.Date.Required", "Please select a date.")]
    [InlineData("hu", "FormValidation.Date.Required", "Kérlek, válassz egy dátumot.")]
    [InlineData("en", "FormValidation.NumericValue.Required", "Please enter a value.")]
    [InlineData("hu", "FormValidation.NumericValue.Required", "Kérlek, adj meg egy értéket.")]
    [InlineData("en", "Mood.Validation.Range", "Mood value must be between 0 and 1.")]
    [InlineData("hu", "Mood.Validation.Range", "A hangulat értékének 0 és 1 között kell lennie.")]
    public void Convert_LocalizesSharedIssuesAndRecognizedMessages(string language, string key, string expected)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            var converter = new ValidationResultToLocalizedConverter();
            object[] errors = [new ValidationIssue("Field", key), key, new FormatException(key)];

            foreach (var error in errors)
            {
                converter.Convert(error, typeof(string), null, CultureInfo.CurrentUICulture).Should().Be(expected);
            }
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("The value is not a number.")]
    [InlineData("Unknown.Validation.Message")]
    [InlineData("")]
    public void Convert_PreservesUnrecognizedMessages(string message)
    {
        var converter = new ValidationResultToLocalizedConverter();
        object[] errors = [message, new FormatException(message)];

        foreach (var error in errors)
        {
            converter.Convert(error, typeof(string), null, CultureInfo.CurrentCulture).Should().Be(message);
        }
    }

    [Fact]
    public void Convert_WithMissingSharedIssueKey_KeepsTheMissingKeyMarker()
    {
        var converter = new ValidationResultToLocalizedConverter();
        var issue = new ValidationIssue("Name", "Unknown.Validation.Message");

        converter.Convert(issue, typeof(string), null, CultureInfo.CurrentCulture)
            .Should().Be("[Unknown.Validation.Message]");
    }

    [Fact]
    public void Convert_WithNull_ReturnsNull()
    {
        var converter = new ValidationResultToLocalizedConverter();

        converter.Convert(null, typeof(string), null, CultureInfo.CurrentCulture).Should().BeNull();
    }
}
