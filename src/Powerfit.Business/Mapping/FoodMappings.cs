using DataAccess.Entities;
using Powerfit.Business.DTOs.Foods;

namespace Powerfit.Business.Mapping;

public static class FoodMappings
{
    public static FoodDto ToDto(this Food f) =>
        new(f.FoodId, f.Name, f.Category, f.SuitableMeals,
            f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g,
            f.IsVegetarian, f.IsGlutenFree, f.ContainsDairy, f.ContainsNuts, f.IsActive, f.Allergens);

    public static Food ToEntity(this CreateFoodRequest r) => new()
    {
        Name = r.Name.Trim(),
        Category = r.Category.Trim(),
        SuitableMeals = MappingHelpers.Clean(r.SuitableMeals),
        CaloriesPer100g = r.CaloriesPer100g,
        ProteinPer100g = r.ProteinPer100g,
        CarbsPer100g = r.CarbsPer100g,
        FatPer100g = r.FatPer100g,
        IsVegetarian = r.IsVegetarian,
        IsGlutenFree = r.IsGlutenFree,
        ContainsDairy = r.ContainsDairy,
        ContainsNuts = r.ContainsNuts,
        Allergens = FoodAllergens.Normalize(r.Allergens),
        IsActive = r.IsActive
    };

    /// <summary>Copies the request onto a tracked entity.</summary>
    public static void Apply(this Food f, UpdateFoodRequest r)
    {
        f.Name = r.Name.Trim();
        f.Category = r.Category.Trim();
        f.SuitableMeals = MappingHelpers.Clean(r.SuitableMeals);
        f.CaloriesPer100g = r.CaloriesPer100g;
        f.ProteinPer100g = r.ProteinPer100g;
        f.CarbsPer100g = r.CarbsPer100g;
        f.FatPer100g = r.FatPer100g;
        f.IsVegetarian = r.IsVegetarian;
        f.IsGlutenFree = r.IsGlutenFree;
        f.ContainsDairy = r.ContainsDairy;
        f.ContainsNuts = r.ContainsNuts;
        f.Allergens = FoodAllergens.Normalize(r.Allergens);
        f.IsActive = r.IsActive;
    }
}
