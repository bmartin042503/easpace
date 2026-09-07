// Copyright (c) 2025 Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace easpace.Desktop.Extensions;

internal static class StringExtensions
{
    extension(string content)
    {
        public int GetWordCount()
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length == 0) return 0;

            // Source - https://stackoverflow.com/a/8784654
            // Posted by vgru, modified by community. See post 'Timeline' for change history
            // Retrieved 2026-09-07, License - CC BY-SA 4.0

            int wordCount = 0, index = 0;

            // skip whitespace until first word
            while (index < content.Length && char.IsWhiteSpace(content[index]))
                index++;

            while (index < content.Length)
            {
                // check if current char is part of a word
                while (index < content.Length && !char.IsWhiteSpace(content[index]))
                    index++;

                wordCount++;

                // skip whitespace until next word
                while (index < content.Length && char.IsWhiteSpace(content[index]))
                    index++;
            }

            return wordCount;
        }
    }
}