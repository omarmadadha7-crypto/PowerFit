// Small value types shared by entities, repositories and services.

using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.Entities;

public readonly record struct NutritionTotals(decimal Calories, decimal ProteinG, decimal CarbsG, decimal FatG)
{
    public static NutritionTotals Zero => default;

    public static NutritionTotals From(
        decimal grams, decimal caloriesPer100g, decimal proteinPer100g, decimal carbsPer100g, decimal fatPer100g)
    {
        var factor = grams / 100m;
        return new NutritionTotals(
            caloriesPer100g * factor,
            proteinPer100g * factor,
            carbsPer100g * factor,
            fatPer100g * factor);
    }

    public NutritionTotals Rounded(int decimals = 1) => new(
        Math.Round(Calories, decimals),
        Math.Round(ProteinG, decimals),
        Math.Round(CarbsG, decimals),
        Math.Round(FatG, decimals));

    public static NutritionTotals operator +(NutritionTotals a, NutritionTotals b) => new(
        a.Calories + b.Calories,
        a.ProteinG + b.ProteinG,
        a.CarbsG + b.CarbsG,
        a.FatG + b.FatG);
}

/// <summary>
/// What a user must avoid. Built from the free-text Profile.DietaryRestrictions (English + Arabic).
/// </summary>
/// <param name="ExcludedAllergens">Values of <see cref="FoodAllergens"/> the user must not eat (optional).</param>
public readonly record struct DietaryFlags(
    bool Vegetarian,
    bool GlutenFree,
    bool NoDairy,
    bool NoNuts,
    IReadOnlyCollection<string>? ExcludedAllergens = null)
{
    public bool HasAny =>
        Vegetarian || GlutenFree || NoDairy || NoNuts || ExcludedAllergens is { Count: > 0 };

    /// <summary>
    /// Keyword based reading of the free text. It is a heuristic (review it for your own data), but it
    /// understands negations such as "non-vegetarian", "not vegetarian" and "غير نباتي".
    /// For allergies it prefers to over-exclude (substring match) rather than to miss one.
    /// </summary>
    public static DietaryFlags FromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return default;

        var allergens = new List<string>();

        // "eggplant" is not an egg; "shellfish" must not count as "fish".
        var forEgg = text.Replace("eggplant", string.Empty, StringComparison.OrdinalIgnoreCase);
        var forFish = text.Replace("shellfish", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (Mentions(forEgg, "egg", "بيض")) allergens.Add(FoodAllergens.Egg);
        if (Mentions(text, "soy", "صويا")) allergens.Add(FoodAllergens.Soy);
        if (Mentions(forFish, "fish", "سمك")) allergens.Add(FoodAllergens.Fish);
        if (Mentions(text, "shellfish", "shrimp", "prawn", "crab", "lobster", "محار", "جمبري", "روبيان", "قريدس"))
            allergens.Add(FoodAllergens.Shellfish);
        if (Mentions(text, "sesame", "tahini", "سمسم", "طحينة", "طحينه")) allergens.Add(FoodAllergens.Sesame);

        return new DietaryFlags(
            Vegetarian: MentionsAffirmed(text, "vegetarian", "vegan", "نباتي", "نباتية"),
            GlutenFree: Mentions(text, "gluten", "celiac", "coeliac", "جلوتين", "غلوتين", "سيلياك"),
            NoDairy: Mentions(text, "dairy", "lactose", "milk", "ألبان", "البان", "لاكتوز", "حليب"),
            NoNuts: Mentions(text, "nuts", "nut allergy", "nut-free", "peanut", "walnut", "hazelnut", "almond", "cashew", "pistachio", "مكسرات", "فول سوداني", "لوز", "كاجو", "فستق"),
            ExcludedAllergens: allergens.Count == 0 ? null : allergens);
    }

    private static bool Mentions(string text, params string[] keys) =>
        keys.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase));

    /// <summary>True when at least one occurrence is not preceded by "non-", "non ", "not " or "غير ".</summary>
    private static bool MentionsAffirmed(string text, params string[] keys)
    {
        foreach (var key in keys)
        {
            var index = 0;
            while ((index = text.IndexOf(key, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                var start = Math.Max(0, index - 4);
                var before = text[start..index];

                var negated =
                    before.EndsWith("non-", StringComparison.OrdinalIgnoreCase) ||
                    before.EndsWith("non ", StringComparison.OrdinalIgnoreCase) ||
                    before.EndsWith("not ", StringComparison.OrdinalIgnoreCase) ||
                    before.EndsWith("غير ", StringComparison.Ordinal);

                if (!negated) return true;
                index += key.Length;
            }
        }

        return false;
    }
}
