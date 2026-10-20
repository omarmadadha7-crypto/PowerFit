using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Profile
{
    public int UserId { get; set; }

    public string? Gender { get; set; }

    public int? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public string? Goal { get; set; }

    public string? ActivityLevel { get; set; }

    public string? ExperienceLevel { get; set; }

    public string? TrainingLocation { get; set; }

    public int? PreferredDaysPerWeek { get; set; }

    public string? Injuries { get; set; }

    public string? DietaryRestrictions { get; set; }

    public bool ProfileCompleted { get; set; }

    public virtual User User { get; set; } = null!;
}
