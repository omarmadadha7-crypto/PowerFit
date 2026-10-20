namespace Powerfit.Business.DTOs.Foods;

public record CreateFoodRequest
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;

    /// <summary>Comma separated meal types, e.g. "Breakfast, Snack". Empty = any meal.</summary>
    public string? SuitableMeals { get; init; }

    public decimal CaloriesPer100g { get; init; }
    public decimal ProteinPer100g { get; init; }
    public decimal CarbsPer100g { get; init; }
    public decimal FatPer100g { get; init; }

    public bool IsVegetarian { get; init; }
    public bool IsGlutenFree { get; init; }
    public bool ContainsDairy { get; init; }
    public bool ContainsNuts { get; init; }

    /// <summary>Comma separated: Egg, Soy, Fish, Shellfish, Sesame (gluten, dairy and nuts have their own flags).</summary>
    public string? Allergens { get; init; }

    public bool IsActive { get; init; } = true;
}
