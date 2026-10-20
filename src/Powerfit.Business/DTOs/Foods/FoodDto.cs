namespace Powerfit.Business.DTOs.Foods;

public record FoodDto(
    int FoodId,
    string Name,
    string Category,
    string? SuitableMeals,
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    bool IsVegetarian,
    bool IsGlutenFree,
    bool ContainsDairy,
    bool ContainsNuts,
    bool IsActive,
    string? Allergens);
