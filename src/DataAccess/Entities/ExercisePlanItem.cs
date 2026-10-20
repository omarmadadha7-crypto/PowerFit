using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class ExercisePlanItem
{
    public int ItemId { get; set; }

    public int ExercisePlanId { get; set; }

    public int ExerciseId { get; set; }

    public int DayNumber { get; set; }

    public int OrderIndex { get; set; }

    public int Sets { get; set; }

    public int Reps { get; set; }

    public string Source { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual Exercise Exercise { get; set; } = null!;

    public virtual ExercisePlan ExercisePlan { get; set; } = null!;
}
