using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Entities;

public partial class FitnessPlan
{
    public FitnessPlan()
    {
        Status = PlanStatuses.Draft;
    }

    // NOTE: IsActive (bool?) is a DB computed column; these helpers read Status instead.
    [NotMapped] public bool IsDraft => Status == PlanStatuses.Draft;
    [NotMapped] public bool IsCurrentlyActive => Status == PlanStatuses.Active;
    [NotMapped] public bool IsArchived => Status == PlanStatuses.Archived;

    public void Activate()
    {
        Status = PlanStatuses.Active;
        Touch();
    }

    public void Archive()
    {
        Status = PlanStatuses.Archived;
        Touch();
    }

    /// <summary>Records that an admin edited the plan.</summary>
    public void MarkEditedByAdmin(int adminId)
    {
        LastEditedByAdminId = adminId;
        LastEditedAt = UtcNowNotBeforeCreation();
        Touch();
    }

    /// <summary>
    /// Updates UpdatedAt. Skipped for brand-new plans so the DB defaults (SYSUTCDATETIME)
    /// fill CreatedAt/UpdatedAt and CK_FitnessPlan_Dates can never be violated by clock skew.
    /// </summary>
    public void Touch()
    {
        if (CreatedAt == default) return;
        UpdatedAt = UtcNowNotBeforeCreation();
    }

    private DateTime UtcNowNotBeforeCreation()
    {
        var now = DateTime.UtcNow;
        return now < CreatedAt ? CreatedAt : now;
    }
}
