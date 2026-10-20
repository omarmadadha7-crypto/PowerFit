using System;
using System.Collections.Generic;
using System.Linq;
using DataAccess.Entities;
using Powerfit.Business.Validators;

namespace Powerfit.Business.AI;

/// <summary>
/// Never trust the model: every id, range and total is checked against the catalogs that were sent,
/// so the database CHECK constraints are never hit with bad data.
/// </summary>
internal static class AiPlanValidator
{
    private const int MaxErrors = 12;

    /// <summary>Cleans the draft (null lists, meal-type casing, note length). Call before Validate.</summary>
    public static void Normalize(AiPlanDraft? draft)
    {
        if (draft is null) return;

        if (draft.ExercisePlan is { } exercisePlan)
        {
            exercisePlan.Items ??= new List<AiExerciseItemDraft>();
            exercisePlan.Items.RemoveAll(i => i is null);
            exercisePlan.Notes = Truncate(exercisePlan.Notes, 500);

            foreach (var item in exercisePlan.Items)
                item.Notes = Truncate(item.Notes, 500);
        }

        if (draft.NutritionPlan is { } nutritionPlan)
        {
            nutritionPlan.Items ??= new List<AiNutritionItemDraft>();
            nutritionPlan.Items.RemoveAll(i => i is null);

            foreach (var item in nutritionPlan.Items)
                item.MealType = CanonicalMealType(item.MealType);
        }
    }

    /// <summary>Returns the list of problems. Empty list = the plan can be saved.</summary>
    public static List<string> Validate(AiPlanDraft? draft, AiPlanRequest request)
    {
        var errors = new List<string>();

        if (draft?.ExercisePlan is null) errors.Add("exercisePlan is missing.");
        if (draft?.NutritionPlan is null) errors.Add("nutritionPlan is missing.");
        if (errors.Count > 0) return errors;

        ValidateExercises(draft!.ExercisePlan!, request, errors);
        ValidateNutrition(draft.NutritionPlan!, request, errors);
        return errors;
    }

    // ------------------------------------------------------------------------

    private static void ValidateExercises(AiExercisePlanDraft plan, AiPlanRequest request, List<string> errors)
    {
        if (plan.Items.Count == 0)
        {
            Add(errors, "exercisePlan.items is empty.");
            return;
        }

        var exerciseIds = request.Exercises.Select(e => e.Id).ToHashSet();

        for (var i = 0; i < plan.Items.Count; i++)
        {
            var item = plan.Items[i];
            var at = $"exercisePlan.items[{i}]";

            if (!exerciseIds.Contains(item.ExerciseId))
                Add(errors, $"{at}: exerciseId {item.ExerciseId} is not in the exercise catalog.");
            if (item.DayNumber is < 1 or > 7)
                Add(errors, $"{at}: dayNumber must be 1-7.");
            if (item.Sets is < 1 or > 20)
                Add(errors, $"{at}: sets must be 1-20.");
            if (item.Reps is < 1 or > 100)
                Add(errors, $"{at}: reps must be 1-100.");
        }

        var trainingDays = plan.Items.Select(i => i.DayNumber).Distinct().Count();
        if (trainingDays != request.DaysPerWeek)
            Add(errors, $"exercisePlan must use exactly {request.DaysPerWeek} training days but uses {trainingDays}.");
    }

    private static void ValidateNutrition(AiNutritionPlanDraft plan, AiPlanRequest request, List<string> errors)
    {
        CheckRange(errors, "nutritionPlan.dailyCalories", plan.DailyCalories, PlanValidator.MinDailyCalories, 6000m);
        CheckRange(errors, "nutritionPlan.proteinG", plan.ProteinG, 0m, 500m);
        CheckRange(errors, "nutritionPlan.carbsG", plan.CarbsG, 0m, 1000m);
        CheckRange(errors, "nutritionPlan.fatG", plan.FatG, 0m, 400m);

        var macroCalories = plan.ProteinG * 4m + plan.CarbsG * 4m + plan.FatG * 9m;
        if (plan.DailyCalories > 0 &&
            Math.Abs(macroCalories - plan.DailyCalories) > plan.DailyCalories * PlanValidator.MacroTolerance)
        {
            Add(errors, $"nutritionPlan macros give {Math.Round(macroCalories)} kcal but dailyCalories is {Math.Round(plan.DailyCalories)}.");
        }

        if (plan.Items.Count == 0)
        {
            Add(errors, "nutritionPlan.items is empty.");
            return;
        }

        var foods = request.Foods.ToDictionary(f => f.Id);

        for (var i = 0; i < plan.Items.Count; i++)
        {
            var item = plan.Items[i];
            var at = $"nutritionPlan.items[{i}]";

            if (!foods.ContainsKey(item.FoodId))
                Add(errors, $"{at}: foodId {item.FoodId} is not in the food catalog.");
            if (item.DayNumber is < 1 or > 7)
                Add(errors, $"{at}: dayNumber must be 1-7.");
            if (!MealTypes.All.Contains(item.MealType))
                Add(errors, $"{at}: mealType '{item.MealType}' must be Breakfast, Lunch, Dinner or Snack.");
            if (item.QuantityGrams < 1m || item.QuantityGrams > 2000m)
                Add(errors, $"{at}: quantityGrams must be 1-2000.");
        }

        var coveredDays = plan.Items.Select(i => i.DayNumber).ToHashSet();
        var missingDays = Enumerable.Range(1, 7).Where(d => !coveredDays.Contains(d)).ToList();
        if (missingDays.Count > 0)
            Add(errors, $"nutritionPlan must cover all 7 days; missing day(s): {string.Join(", ", missingDays)}.");

        // Real calories of every day (from the catalog values) must be close to the target.
        if (plan.DailyCalories > 0)
        {
            var validItems = plan.Items.Where(i => foods.ContainsKey(i.FoodId) && i.DayNumber is >= 1 and <= 7);

            foreach (var day in validItems.GroupBy(i => i.DayNumber).OrderBy(g => g.Key))
            {
                var kcal = day.Sum(i => foods[i.FoodId].Kcal * i.QuantityGrams / 100m);
                if (Math.Abs(kcal - plan.DailyCalories) > plan.DailyCalories * PlanValidator.DayCalorieTolerance)
                    Add(errors, $"nutritionPlan day {day.Key} has about {Math.Round(kcal)} kcal but dailyCalories is {Math.Round(plan.DailyCalories)}.");
            }
        }
    }

    // ------------------------------------------------------------------------

    private static void CheckRange(List<string> errors, string field, decimal value, decimal min, decimal max)
    {
        if (value < min || value > max)
            Add(errors, $"{field} must be between {min} and {max}.");
    }

    private static void Add(List<string> errors, string message)
    {
        if (errors.Count < MaxErrors) errors.Add(message);
    }

    private static string CanonicalMealType(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return MealTypes.All.FirstOrDefault(m => string.Equals(m, trimmed, StringComparison.OrdinalIgnoreCase)) ?? trimmed;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
