using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Exercise
{
    public int ExerciseId { get; set; }

    public string Name { get; set; } = null!;

    public string MuscleGroup { get; set; } = null!;

    public string ExerciseType { get; set; } = null!;

    public string Equipment { get; set; } = null!;

    public string DifficultyLevel { get; set; } = null!;

    public string? ContraindicatedAreas { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<ExercisePlanItem> ExercisePlanItems { get; set; } = new List<ExercisePlanItem>();
}
