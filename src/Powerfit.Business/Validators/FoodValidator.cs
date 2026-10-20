using DataAccess.Entities;

namespace Powerfit.Business.Validators;

public static class FoodValidator
{
    /// <summary>Shared by Create and Update (same fields, same rules as the SQL CHECKs).</summary>
    public static void Validate(
        string? name,
        string? category,
        string? suitableMeals,
        decimal caloriesPer100g,
        decimal proteinPer100g,
        decimal carbsPer100g,
        decimal fatPer100g,
        string? allergens = null)
    {
        var v = new ValidationBuilder();
        var unknownAllergens = FoodAllergens.Unknown(allergens);
        v.Required("name", name, 100)
         .Required("category", category, 30)
         .Optional("suitableMeals", suitableMeals, 100)
         .Optional("allergens", allergens, 100)
         .Check(unknownAllergens.Count == 0, "allergens",
                $"allergens has unknown value(s): {string.Join(", ", unknownAllergens)}. Allowed: {string.Join(", ", FoodAllergens.All)}.")
         .DecimalRange("caloriesPer100g", caloriesPer100g, 0m, 900m)
         .DecimalRange("proteinPer100g", proteinPer100g, 0m, 100m)
         .DecimalRange("carbsPer100g", carbsPer100g, 0m, 100m)
         .DecimalRange("fatPer100g", fatPer100g, 0m, 100m)
         .Check(proteinPer100g + carbsPer100g + fatPer100g <= 100m, "macros",
                "protein + carbs + fat per 100g cannot exceed 100.");
        v.ThrowIfInvalid();
    }
}
