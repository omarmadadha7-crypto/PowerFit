using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.Entities;

/// <summary>Splits free-text columns ("knee, lower back") into clean distinct tokens.</summary>
internal static class TextList
{
    private static readonly char[] Separators = [',', ';', '|', '/', '\n', '\r', '،', '؛'];

    public static IReadOnlyList<string> Split(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

        return text
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
