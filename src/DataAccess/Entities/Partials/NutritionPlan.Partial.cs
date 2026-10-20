using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DataAccess.Entities;

public partial class NutritionPlan
{
    /// <summary>Calories implied by the macros: 4 / 4 / 9 kcal per gram.</summary>
    [NotMapped] public int MacroCalories => ProteinG * 4 + CarbsG * 4 + FatG * 9;

    /// <summary>True when macro calories are within the tolerance (default 10%) of DailyCalories.</summary>
    public bool IsMacroConsistent(decimal tolerance = 0.10m) =>
        DailyCalories > 0 && Math.Abs(MacroCalories - DailyCalories) <= DailyCalories * tolerance;

    /// <summary>Requires NutritionPlanItems and their Food to be loaded.</summary>
    public NutritionTotals GetDayTotals(int dayNumber) => NutritionPlanItems
        .Where(i => i.DayNumber == dayNumber)
        .Aggregate(NutritionTotals.Zero, (sum, item) => sum + item.GetTotals())
        .Rounded();

    /// <summary>Requires NutritionPlanItems and their Food to be loaded.</summary>
    public Dictionary<int, NutritionTotals> GetWeeklyTotals() => NutritionPlanItems
        .GroupBy(i => i.DayNumber)
        .ToDictionary(
            g => g.Key,
            g => g.Aggregate(NutritionTotals.Zero, (sum, item) => sum + item.GetTotals()).Rounded());

    public IEnumerable<NutritionPlanItem> GetMeal(int dayNumber, string mealType) => NutritionPlanItems
        .Where(i => i.DayNumber == dayNumber && i.MealType == mealType);
}
