using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DataAccess.Entities;

public partial class Food
{
    public Food()
    {
        IsActive = true;
    }

    [NotMapped] public IReadOnlyList<string> SuitableMealList => TextList.Split(SuitableMeals);
    [NotMapped] public IReadOnlyList<string> AllergenList => TextList.Split(Allergens);

    public bool ContainsAnyAllergen(IEnumerable<string> allergens) =>
        AllergenList.Intersect(allergens, StringComparer.OrdinalIgnoreCase).Any();

    /// <summary>Foods without SuitableMeals are considered suitable for any meal.</summary>
    public bool IsSuitableForMeal(string mealType)
    {
        var meals = SuitableMealList;
        return meals.Count == 0 || meals.Contains(mealType, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsCompatibleWith(DietaryFlags flags) =>
        (!flags.Vegetarian || IsVegetarian)
        && (!flags.GlutenFree || IsGlutenFree)
        && (!flags.NoDairy || !ContainsDairy)
        && (!flags.NoNuts || !ContainsNuts)
        && (flags.ExcludedAllergens is not { Count: > 0 } excluded || !ContainsAnyAllergen(excluded));

    public NutritionTotals CalculateFor(decimal grams) =>
        NutritionTotals.From(grams, CaloriesPer100g, ProteinPer100g, CarbsPer100g, FatPer100g);
}
