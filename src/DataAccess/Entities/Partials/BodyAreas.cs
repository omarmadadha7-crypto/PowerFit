using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DataAccess.Entities;

/// <summary>
/// Language independent body areas (English + Arabic) used to compare a user's injuries with
/// Exercise.ContraindicatedAreas. Both texts are reduced to canonical areas first, so "الركبة" and "knee" match.
/// Best-effort dictionary: terms it does not know fall back to plain text matching in Exercise.IsSafeFor.
/// </summary>
public static class BodyAreas
{
    public const string Knee = "Knee";
    public const string Ankle = "Ankle";
    public const string Hip = "Hip";
    public const string LowerBack = "Lower Back";
    public const string UpperBack = "Upper Back";
    public const string Neck = "Neck";
    public const string Shoulder = "Shoulder";
    public const string Elbow = "Elbow";
    public const string Wrist = "Wrist";
    public const string Foot = "Foot";
    public const string Chest = "Chest";
    public const string Hamstring = "Hamstring";
    public const string Calf = "Calf";

    public static readonly string[] All =
        [Knee, Ankle, Hip, LowerBack, UpperBack, Neck, Shoulder, Elbow, Wrist, Foot, Chest, Hamstring, Calf];

    private sealed record Rule(string[] Areas, string[] Terms, bool Generic = false);

    // Terms are written the way Normalize() writes text (no diacritics, no "ال", alef/yaa/taa-marbuta unified).
    // "Generic" rules (plain "back", "spine") apply only when no specific back area was found.
    private static readonly Rule[] Rules =
    [
        new([Knee], ["knee", "knees", "patella", "patellar", "acl", "mcl", "meniscus", "cruciate", "ركب", "صليبي"]),
        new([Ankle], ["ankle", "ankles", "achilles", "كاحل"]),
        new([Hip], ["hip", "hips", "pelvis", "groin", "ورك", "حوض"]),
        new([LowerBack], ["lower back", "lumbar", "lumbago", "sciatica", "اسفل ظهر", "قطن", "عرق نسا"]),
        new([UpperBack], ["upper back", "mid back", "thoracic", "اعلي ظهر", "وسط ظهر"]),
        new([Neck], ["neck", "cervical", "whiplash", "رقب", "عنق"]),
        new([Shoulder], ["shoulder", "shoulders", "rotator", "كتف", "اكتاف"]),
        new([Elbow], ["elbow", "elbows", "مرفق", "مرافق", "كوع"]),
        new([Wrist], ["wrist", "wrists", "carpal", "رسغ", "معصم"]),
        new([Foot], ["foot", "feet", "heel", "plantar", "قدم", "اقدام", "كعب"]),
        new([Chest], ["chest", "pectoral", "rib", "ribs", "sternum", "صدر", "ضلع", "ضلوع"]),
        new([Hamstring], ["hamstring", "hamstrings"]),
        new([Calf], ["calf", "calves", "shin", "shins", "سمانه", "ساق"]),
        new([LowerBack, UpperBack],
            ["back", "spine", "spinal", "disc", "disk", "ظهر", "عمود فقري", "ديسك", "انزلاق غضروفي"], Generic: true)
    ];

    private sealed record Compiled(string[] Areas, string[] LatinTerms, string[] ArabicTerms, bool Generic);

    private static readonly Compiled[] CompiledRules = Rules
        .Select(r =>
        {
            var terms = r.Terms.Select(Normalize).ToArray();
            return new Compiled(
                r.Areas,
                terms.Where(t => !IsArabic(t)).ToArray(),
                terms.Where(IsArabic).ToArray(),
                r.Generic);
        })
        .ToArray();

    /// <summary>Canonical areas mentioned in the text (empty when nothing is recognised).</summary>
    public static IReadOnlySet<string> Resolve(string? text)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text)) return result;

        var normalized = Normalize(text);
        var words = " " + Regex.Replace(normalized, @"[^\p{L}\p{N}]+", " ").Trim() + " ";

        foreach (var rule in CompiledRules.Where(r => !r.Generic))
            Apply(rule, normalized, words, result);

        if (!result.Contains(LowerBack) && !result.Contains(UpperBack))
        {
            foreach (var rule in CompiledRules.Where(r => r.Generic))
                Apply(rule, normalized, words, result);
        }

        return result;
    }

    public static IReadOnlySet<string> ResolveAll(IEnumerable<string> texts)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var text in texts) result.UnionWith(Resolve(text));
        return result;
    }

    private static void Apply(Compiled rule, string normalized, string words, HashSet<string> result)
    {
        // Latin terms: whole words/phrases ("hip" must not match "ship"). Arabic terms: stems (substring).
        var hit = rule.LatinTerms.Any(t => words.Contains(" " + t + " ", StringComparison.Ordinal))
                  || rule.ArabicTerms.Any(t => normalized.Contains(t, StringComparison.Ordinal));

        if (hit) result.UnionWith(rule.Areas);
    }

    private static bool IsArabic(string term) => term.Any(c => c is >= '\u0600' and <= '\u06FF');

    internal static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);

        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            switch (ch)
            {
                case '\u0640':                       // tatweel
                case '\u0670':                       // dagger alef
                case >= '\u064B' and <= '\u065F':    // harakat
                    continue;
                case 'أ' or 'إ' or 'آ' or 'ٱ':
                    sb.Append('ا');
                    break;
                case 'ى':
                    sb.Append('ي');
                    break;
                case 'ة':
                    sb.Append('ه');
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        // Drop the definite article at the start of a word: "الظهر" -> "ظهر", "اسفل الظهر" -> "اسفل ظهر".
        return Regex.Replace(sb.ToString(), @"(?<!\p{L})ال(?=\p{L}{2,})", string.Empty);
    }
}
