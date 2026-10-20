using System;
using System.Collections.Generic;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public partial class PowerFitDbContext : DbContext
{
    public PowerFitDbContext(DbContextOptions<PowerFitDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AigenerationLog> AigenerationLogs { get; set; }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<ExercisePlan> ExercisePlans { get; set; }

    public virtual DbSet<ExercisePlanItem> ExercisePlanItems { get; set; }

    public virtual DbSet<FitnessPlan> FitnessPlans { get; set; }

    public virtual DbSet<Food> Foods { get; set; }

    public virtual DbSet<NutritionPlan> NutritionPlans { get; set; }

    public virtual DbSet<NutritionPlanItem> NutritionPlanItems { get; set; }

    public virtual DbSet<Person> People { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("Admin");

            entity.HasIndex(e => e.PersonId, "UQ_Admin_PersonID").IsUnique();

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.PersonId).HasColumnName("PersonID");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .HasDefaultValue("Admin", "DF_Admin_Role");

            entity.HasOne(d => d.Person).WithMany(p => p.Admins)
                .HasPrincipalKey(p => new { p.PersonId, p.Role })
                .HasForeignKey(d => new { d.PersonId, d.Role })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Admin_Person");
        });

        modelBuilder.Entity<AigenerationLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("AIGenerationLog");

            entity.HasIndex(e => new { e.PlanId, e.CreatedAt }, "IX_AIGenerationLog_PlanID_CreatedAt");

            entity.Property(e => e.LogId).HasColumnName("LogID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AIGenerationLog_CreatedAt");
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Plan).WithMany(p => p.AigenerationLogs)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_AIGenerationLog_FitnessPlan");
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("Exercise");

            entity.HasIndex(e => e.Name, "UQ_Exercise_Name").IsUnique();

            entity.Property(e => e.ExerciseId).HasColumnName("ExerciseID");
            entity.Property(e => e.ContraindicatedAreas).HasMaxLength(200);
            entity.Property(e => e.DifficultyLevel).HasMaxLength(20);
            entity.Property(e => e.Equipment)
                .HasMaxLength(30)
                .HasDefaultValue("None", "DF_Exercise_Equipment");
            entity.Property(e => e.ExerciseType).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Exercise_IsActive");
            entity.Property(e => e.MuscleGroup).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ExercisePlan>(entity =>
        {
            entity.ToTable("ExercisePlan");

            entity.HasIndex(e => e.PlanId, "UQ_ExercisePlan_PlanID").IsUnique();

            entity.Property(e => e.ExercisePlanId).HasColumnName("ExercisePlanID");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PlanId).HasColumnName("PlanID");

            entity.HasOne(d => d.Plan).WithOne(p => p.ExercisePlan)
                .HasForeignKey<ExercisePlan>(d => d.PlanId)
                .HasConstraintName("FK_ExercisePlan_FitnessPlan");
        });

        modelBuilder.Entity<ExercisePlanItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.ToTable("ExercisePlanItem");

            entity.HasIndex(e => e.ExerciseId, "IX_ExercisePlanItem_ExerciseID");

            entity.HasIndex(e => new { e.ExercisePlanId, e.DayNumber, e.OrderIndex }, "UQ_ExercisePlanItem_Order").IsUnique();

            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.ExerciseId).HasColumnName("ExerciseID");
            entity.Property(e => e.ExercisePlanId).HasColumnName("ExercisePlanID");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Source)
                .HasMaxLength(10)
                .HasDefaultValue("AI", "DF_ExercisePlanItem_Source");

            entity.HasOne(d => d.Exercise).WithMany(p => p.ExercisePlanItems)
                .HasForeignKey(d => d.ExerciseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExercisePlanItem_Exercise");

            entity.HasOne(d => d.ExercisePlan).WithMany(p => p.ExercisePlanItems)
                .HasForeignKey(d => d.ExercisePlanId)
                .HasConstraintName("FK_ExercisePlanItem_ExercisePlan");
        });

        modelBuilder.Entity<FitnessPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId);

            entity.ToTable("FitnessPlan");

            entity.HasIndex(e => e.LastEditedByAdminId, "IX_FitnessPlan_LastEditedByAdminID");

            entity.HasIndex(e => e.UserId, "IX_FitnessPlan_UserID");

            entity.Property(e => e.PlanId).HasColumnName("PlanID");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_FitnessPlan_CreatedAt");
            entity.Property(e => e.IsActive).HasComputedColumnSql("(CONVERT([bit],case when [Status]=N'Active' then (1) else (0) end))", true);
            entity.Property(e => e.LastEditedAt).HasPrecision(0);
            entity.Property(e => e.LastEditedByAdminId).HasColumnName("LastEditedByAdminID");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Draft", "DF_FitnessPlan_Status");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_FitnessPlan_UpdatedAt");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.LastEditedByAdmin).WithMany(p => p.FitnessPlans)
                .HasForeignKey(d => d.LastEditedByAdminId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_FitnessPlan_Admin");

            entity.HasOne(d => d.User).WithMany(p => p.FitnessPlans)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_FitnessPlan_User");
        });

        modelBuilder.Entity<Food>(entity =>
        {
            entity.ToTable("Food");

            entity.HasIndex(e => e.Name, "UQ_Food_Name").IsUnique();

            entity.Property(e => e.FoodId).HasColumnName("FoodID");
            entity.Property(e => e.CaloriesPer100g).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.CarbsPer100g).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.Category).HasMaxLength(30);
            entity.Property(e => e.FatPer100g).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Food_IsActive");
            entity.Property(e => e.Allergens).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ProteinPer100g).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.SuitableMeals).HasMaxLength(100);
        });

        modelBuilder.Entity<NutritionPlan>(entity =>
        {
            entity.ToTable("NutritionPlan");

            entity.HasIndex(e => e.PlanId, "UQ_NutritionPlan_PlanID").IsUnique();

            entity.Property(e => e.NutritionPlanId).HasColumnName("NutritionPlanID");
            entity.Property(e => e.PlanId).HasColumnName("PlanID");

            entity.HasOne(d => d.Plan).WithOne(p => p.NutritionPlan)
                .HasForeignKey<NutritionPlan>(d => d.PlanId)
                .HasConstraintName("FK_NutritionPlan_FitnessPlan");
        });

        modelBuilder.Entity<NutritionPlanItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.ToTable("NutritionPlanItem");

            entity.HasIndex(e => e.FoodId, "IX_NutritionPlanItem_FoodID");

            entity.Property(e => e.ItemId).HasColumnName("ItemID");
            entity.Property(e => e.FoodId).HasColumnName("FoodID");
            entity.Property(e => e.MealType).HasMaxLength(20);
            entity.Property(e => e.NutritionPlanId).HasColumnName("NutritionPlanID");
            entity.Property(e => e.Source)
                .HasMaxLength(10)
                .HasDefaultValue("AI", "DF_NutritionPlanItem_Source");

            entity.HasOne(d => d.Food).WithMany(p => p.NutritionPlanItems)
                .HasForeignKey(d => d.FoodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NutritionPlanItem_Food");

            entity.HasOne(d => d.NutritionPlan).WithMany(p => p.NutritionPlanItems)
                .HasForeignKey(d => d.NutritionPlanId)
                .HasConstraintName("FK_NutritionPlanItem_NutritionPlan");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("Person");

            entity.HasIndex(e => e.Email, "UQ_Person_Email").IsUnique();

            entity.HasIndex(e => new { e.PersonId, e.Role }, "UQ_Person_PersonID_Role").IsUnique();

            entity.Property(e => e.PersonId).HasColumnName("PersonID");
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(10);
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("Profile");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("UserID");
            entity.Property(e => e.ActivityLevel).HasMaxLength(20);
            entity.Property(e => e.DietaryRestrictions).HasMaxLength(500);
            entity.Property(e => e.ExperienceLevel).HasMaxLength(20);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Goal).HasMaxLength(30);
            entity.Property(e => e.Injuries).HasMaxLength(500);
            entity.Property(e => e.TrainingLocation).HasMaxLength(20);
            entity.Property(e => e.WeightKg).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.User).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.UserId)
                .HasConstraintName("FK_Profile_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.HasIndex(e => e.PersonId, "UQ_User_PersonID").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.PersonId).HasColumnName("PersonID");
            entity.Property(e => e.Role)
                .HasMaxLength(10)
                .HasDefaultValue("User", "DF_User_Role");

            entity.HasOne(d => d.Person).WithMany(p => p.Users)
                .HasPrincipalKey(p => new { p.PersonId, p.Role })
                .HasForeignKey(d => new { d.PersonId, d.Role })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Person");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
