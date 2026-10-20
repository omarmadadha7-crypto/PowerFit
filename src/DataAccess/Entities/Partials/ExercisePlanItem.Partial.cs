using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities;

public partial class ExercisePlanItem
{
    public ExercisePlanItem()
    {
        Source = ItemSources.AI;
    }

    [NotMapped] public int TotalReps => Sets * Reps;
    [NotMapped] public bool IsAdminEdited => Source == ItemSources.Admin;
}
