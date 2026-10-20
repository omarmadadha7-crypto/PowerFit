using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class ExercisePlan
{
    public int ExercisePlanId { get; set; }

    public int PlanId { get; set; }

    public int DaysPerWeek { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<ExercisePlanItem> ExercisePlanItems { get; set; } = new List<ExercisePlanItem>();

    public virtual FitnessPlan Plan { get; set; } = null!;
}
