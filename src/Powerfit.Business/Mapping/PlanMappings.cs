using System;
using System.Collections.Generic;
using System.Linq;
using DataAccess.Entities;
using Powerfit.Business.DTOs.Plans;

namespace Powerfit.Business.Mapping;

/// <summary>
/// The plan graph must be loaded first: ExercisePlan -> Items -> Exercise and NutritionPlan -> Items -> Food
/// (use IFitnessPlanRepository.GetWithDetailsAsync).
/// </summary>
public static class PlanMappings
{
    // ---------------- plan ----------------

    public static FitnessPlanDto ToDto(this FitnessPlan plan) =>
        new(plan.PlanId,
            plan.UserId,
            plan.Status,
            plan.Status == PlanStatuses.Active,
            plan.CreatedAt,
            plan.UpdatedAt,
            plan.LastEditedAt,
            plan.LastEditedByAdminId,
            plan.ExercisePlan?.ToDto(),
            plan.NutritionPlan?.ToDto());

    /// <summary>Works without details; UserFullName is filled only if User -> Person was loaded.</summary>
    public static FitnessPlanSummaryDto ToSummaryDto(this FitnessPlan plan) =>
        new(plan.PlanId,
            plan.UserId,
            plan.User?.Person?.FullName,
            plan.Status,
            plan.CreatedAt,
            plan.UpdatedAt,
            plan.LastEditedAt,
            plan.LastEditedByAdminId);

    // ---------------- exercises ----------------

    public static ExercisePlanDto ToDto(this ExercisePlan plan)
    {
        var days = plan.ExercisePlanItems
            .OrderBy(i => i.DayNumber).ThenBy(i => i.OrderIndex)
            .GroupBy(i => i.DayNumber)
            .Select(g => new ExercisePlanDayDto(g.Key, g.Select(i => i.ToDto()).ToList()))
            .ToList();

        return new ExercisePlanDto(plan.ExercisePlanId, plan.DaysPerWeek, plan.Notes, days);
    }

    public static ExercisePlanItemDto ToDto(this ExercisePlanItem item)
    {
        var exercise = item.Exercise
            ?? throw new InvalidOperationException("Exercise navigation is not loaded for this plan item.");

        return new ExercisePlanItemDto(
            item.ItemId,
            item.ExerciseId,
            exercise.Name,
            exercise.MuscleGroup,
            exercise.ExerciseType,
            exercise.Equipment,
            item.DayNumber,
            item.OrderIndex,
            item.Sets,
            item.Reps,
            item.Source,
            item.Notes);
    }

    // ---------------- nutrition ----------------

    public static NutritionPlanDto ToDto(this NutritionPlan plan)
    {
        var days = plan.NutritionPlanItems
            .GroupBy(i => i.DayNumber)
            .OrderBy(g => g.Key)
            .Select(day => new NutritionDayDto(
                day.Key,
                SumOf(day),
                day.GroupBy(i => i.MealType)
                   .OrderBy(m => MealTypes.Order(m.Key))
                   .Select(m => new MealDto(
                       m.Key,
                       SumOf(m),
                       m.OrderBy(i => i.ItemId).Select(i => i.ToDto()).ToList()))
                   .ToList()))
            .ToList();

        return new NutritionPlanDto(
            plan.NutritionPlanId,
            plan.DailyCalories,
            plan.ProteinG,
            plan.CarbsG,
            plan.FatG,
            days);
    }

    public static NutritionPlanItemDto ToDto(this NutritionPlanItem item)
    {
        var food = item.Food
            ?? throw new InvalidOperationException("Food navigation is not loaded for this plan item.");

        return new NutritionPlanItemDto(
            item.ItemId,
            item.FoodId,
            food.Name,
            food.Category,
            item.DayNumber,
            item.MealType,
            item.QuantityGrams,
            item.GetTotals().Rounded().ToDto(),
            item.Source);
    }

    public static MacroTotalsDto ToDto(this NutritionTotals totals) =>
        new(totals.Calories, totals.ProteinG, totals.CarbsG, totals.FatG);

    private static MacroTotalsDto SumOf(IEnumerable<NutritionPlanItem> items) =>
        items.Aggregate(NutritionTotals.Zero, (sum, item) => sum + item.GetTotals())
             .Rounded()
             .ToDto();
}
