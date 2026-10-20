namespace Powerfit.Business.DTOs.Foods;

public record FoodQuery
{
    public string? Term { get; init; }
    public string? Category { get; init; }
    public string? MealType { get; init; }

    /// <summary>Null = both active and inactive.</summary>
    public bool? IsActive { get; init; }

    public bool VegetarianOnly { get; init; }
    public bool GlutenFreeOnly { get; init; }
    public bool ExcludeDairy { get; init; }
    public bool ExcludeNuts { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
