using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class AigenerationLog
{
    public int LogId { get; set; }

    public int PlanId { get; set; }

    public string Model { get; set; } = null!;

    public string RequestJson { get; set; } = null!;

    public string? ResponseJson { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual FitnessPlan Plan { get; set; } = null!;
}
