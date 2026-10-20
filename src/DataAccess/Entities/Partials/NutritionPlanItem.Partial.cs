using System;

namespace DataAccess.Entities;

public partial class NutritionPlanItem
{
    public NutritionPlanItem()
    {
        Source = ItemSources.AI;
    }

    /// <summary>Macros of this item (QuantityGrams of Food). Food must be loaded.</summary>
    public NutritionTotals GetTotals()
    {
        if (Food is null)
            throw new InvalidOperationException("Food navigation is not loaded. Use .Include(i => i.Food).");

        return Food.CalculateFor(QuantityGrams);
    }
}
