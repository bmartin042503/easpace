// Copyright (c) 2026 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace easpace.Desktop.Extensions;

internal static class TimeSpanExtensions
{
    extension(TimeSpan timeSpan)
    {
        // mm:ss under an hour, h:mm:ss (total hours) from an hour
        public string ToDurationText()
        {
            return timeSpan.TotalHours >= 1
                ? FormattableString.Invariant(
                    $"{(long)timeSpan.TotalHours}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}")
                : FormattableString.Invariant(
                    $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}");
        }
    }
}