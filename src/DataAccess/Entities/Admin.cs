using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Admin
{
    public int AdminId { get; set; }

    public int PersonId { get; set; }

    public string Role { get; set; } = null!;

    public virtual ICollection<FitnessPlan> FitnessPlans { get; set; } = new List<FitnessPlan>();

    public virtual Person Person { get; set; } = null!;
}
