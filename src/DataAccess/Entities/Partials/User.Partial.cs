using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DataAccess.Entities;

public partial class User
{
    public User()
    {
        Role = UserRoles.User; // must match Person.Role (composite FK + CHECK)
    }

    [NotMapped] public bool HasCompletedProfile => Profile?.ProfileCompleted == true;

    /// <summary>Requires FitnessPlans to be loaded.</summary>
    public FitnessPlan? GetActivePlan() => FitnessPlans
        .Where(p => p.Status == PlanStatuses.Active)
        .OrderByDescending(p => p.UpdatedAt)
        .FirstOrDefault();
}
