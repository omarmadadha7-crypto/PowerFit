using System.Collections.Generic;

namespace Powerfit.Business.DTOs.Plans;

public record NutritionPlanDto(
    int NutritionPlanId,
    int DailyCalories,
    int ProteinG,
    int CarbsG,
    int FatG,
    IReadOnlyList<NutritionDayDto> Days);

public record NutritionDayDto(
    int DayNumber,
    MacroTotalsDto Totals,
    IReadOnlyList<MealDto> Meals);

public record MealDto(
    string MealType,
    MacroTotalsDto Totals,
    IReadOnlyList<NutritionPlanItemDto> Items);

public record MacroTotalsDto(
    decimal Calories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG);
