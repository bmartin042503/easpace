// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using easpace.Desktop.Converters;
using easpace.Desktop.Validation;
using FluentAssertions;

namespace easpace.Tests.Validation;

public class ValidationResultToLocalizedConverterTests
{
    [Theory]
    [InlineData("en", "Please enter a name.")]
    [InlineData("hu", "Kérlek, adj meg egy nevet.")]
    public void Convert_LocalizesSharedIssuesAndLegacyErrors(string language, string expected)
    {
        var previousCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            const string key = "FormValidation.Name.Required";
            var converter = new ValidationResultToLocalizedConverter();
            object[] errors = [new ValidationIssue("Name", key), new ValidationResult(key), key, new ValidationException(key)];

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
        object[] errors = [message, new ValidationResult(message), new FormatException(message)];

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
