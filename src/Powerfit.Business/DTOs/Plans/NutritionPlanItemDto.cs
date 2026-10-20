namespace Powerfit.Business.DTOs.Plans;

public record NutritionPlanItemDto(
    int ItemId,
    int FoodId,
    string FoodName,
    string Category,
    int DayNumber,
    string MealType,
    int QuantityGrams,
    MacroTotalsDto Macros,
    string Source);
