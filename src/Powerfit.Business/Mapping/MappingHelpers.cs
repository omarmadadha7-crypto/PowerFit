using System;
using System.Collections.Generic;
using System.Linq;

namespace Powerfit.Business.Mapping;

internal static class MappingHelpers
{
    /// <summary>Trims; blank becomes null (the DB CHECKs reject blank strings).</summary>
    public static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Returns the allowed value with the canonical casing ("male" -> "Male").</summary>
    public static string Canonical(string value, IEnumerable<string> allowed)
    {
        var trimmed = value.Trim();
        return allowed.FirstOrDefault(a => string.Equals(a, trimmed, StringComparison.OrdinalIgnoreCase)) ?? trimmed;
    }

    public static string? CanonicalOrNull(string? value, IEnumerable<string> allowed) =>
        Clean(value) is { } cleaned ? Canonical(cleaned, allowed) : null;
}
