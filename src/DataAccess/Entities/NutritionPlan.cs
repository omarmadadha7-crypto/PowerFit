using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class NutritionPlan
{
    public int NutritionPlanId { get; set; }

    public int PlanId { get; set; }

    public int DailyCalories { get; set; }

    public int ProteinG { get; set; }

    public int CarbsG { get; set; }

    public int FatG { get; set; }

    public virtual ICollection<NutritionPlanItem> NutritionPlanItems { get; set; } = new List<NutritionPlanItem>();

    public virtual FitnessPlan Plan { get; set; } = null!;
}
