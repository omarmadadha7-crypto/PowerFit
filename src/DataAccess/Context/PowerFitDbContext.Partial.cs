using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public partial class PowerFitDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // IsActive has DEFAULT 1 in SQL. By default EF treats `false` as "not set" and omits it,
        // so the DB would silently insert 1. Using `true` as the sentinel fixes that:
        // false is now really inserted as 0.
        modelBuilder.Entity<Exercise>().Property(e => e.IsActive).HasSentinel(true);
        modelBuilder.Entity<Food>().Property(e => e.IsActive).HasSentinel(true);

        // "One Active plan per user" is enforced by the database (see Database/001_*.sql).
        // Declared here so the model documents it; FitnessPlanRepository.ActivateAsync relies on it.
        modelBuilder.Entity<FitnessPlan>()
            .HasIndex(p => p.UserId, "UX_FitnessPlan_OneActivePerUser")
            .IsUnique()
            .HasFilter("[Status] = N'Active'");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyEntityRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyEntityRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Rules applied automatically right before every save:
    ///  - Person: trim names / lower-case e-mail
    ///  - Profile: keep ProfileCompleted consistent with the data
    ///  - FitnessPlan: bump UpdatedAt on modification (unless it was set explicitly)
    /// </summary>
    private void ApplyEntityRules()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            switch (entry.Entity)
            {
                case Person person:
                    person.Normalize();
                    break;

                case Profile profile:
                    profile.RefreshCompletion();
                    break;

                case FitnessPlan plan
                    when entry.State == EntityState.Modified
                         && !entry.Property(nameof(FitnessPlan.UpdatedAt)).IsModified:
                    plan.Touch();
                    break;
            }
        }
    }
}
