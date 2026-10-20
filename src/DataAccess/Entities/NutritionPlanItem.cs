using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class NutritionPlanItem
{
    public int ItemId { get; set; }

    public int NutritionPlanId { get; set; }

    public int FoodId { get; set; }

    public int DayNumber { get; set; }

    public string MealType { get; set; } = null!;

    public int QuantityGrams { get; set; }

    public string Source { get; set; } = null!;

    public virtual Food Food { get; set; } = null!;

    public virtual NutritionPlan NutritionPlan { get; set; } = null!;
}
