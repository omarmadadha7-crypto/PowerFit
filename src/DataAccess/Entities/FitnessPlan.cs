using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class FitnessPlan
{
    public int PlanId { get; set; }

    public int UserId { get; set; }

    public int? LastEditedByAdminId { get; set; }

    public string Status { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? LastEditedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<AigenerationLog> AigenerationLogs { get; set; } = new List<AigenerationLog>();

    public virtual ExercisePlan? ExercisePlan { get; set; }

    public virtual Admin? LastEditedByAdmin { get; set; }

    public virtual NutritionPlan? NutritionPlan { get; set; }

    public virtual User User { get; set; } = null!;
}
