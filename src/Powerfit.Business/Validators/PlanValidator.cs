using System;
using Powerfit.Business.DTOs.Plans;

namespace Powerfit.Business.Validators;

public static class PlanValidator
{
    /// <summary>Max relative gap between DailyCalories and (4*protein + 4*carbs + 9*fat).</summary>
    public const decimal MacroTolerance = 0.15m;

    /// <summary>Max relative gap between a day's real calories and DailyCalories (AI output check).</summary>
    public const decimal DayCalorieTolerance = 0.30m;

    /// <summary>Lowest daily calorie target accepted anywhere (AI output and admin edits). Same value as the AI prompt.</summary>
    public const int MinDailyCalories = 1200;

    public static void Validate(GeneratePlanRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.IntRange("daysPerWeek", r.DaysPerWeek, 1, 7)
         .Optional("additionalNotes", r.AdditionalNotes, 1000)
         .Optional("language", r.Language, 10);
        v.ThrowIfInvalid();
    }

    public static void ValidateStatus(string? status)
    {
        var v = new ValidationBuilder();
        v.OneOf("status", status, AllowedValues.PlanStatusValues);
        v.ThrowIfInvalid();
    }

    // ---------------- admin editing: exercises ----------------

    public static void Validate(UpdateExercisePlanRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.Check(r.DaysPerWeek is not null || r.Notes is not null, "request", "Provide daysPerWeek and/or notes.")
         .IntRange("daysPerWeek", r.DaysPerWeek, 1, 7)
         .Optional("notes", r.Notes, 500);
        v.ThrowIfInvalid();
    }

    public static void Validate(AddExercisePlanItemRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.IntRange("exerciseId", r.ExerciseId, 1, int.MaxValue, required: true)
         .IntRange("dayNumber", r.DayNumber, 1, 7, required: true)
         .IntRange("orderIndex", r.OrderIndex, 1, 1000)
         .IntRange("sets", r.Sets, 1, 20, required: true)
         .IntRange("reps", r.Reps, 1, 100, required: true)
         .Optional("notes", r.Notes, 500);
        v.ThrowIfInvalid();
    }

    public static void Validate(UpdateExercisePlanItemRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.Check(r.ExerciseId is not null || r.Sets is not null || r.Reps is not null || r.Notes is not null,
                "request", "Provide at least one field to change.")
         .IntRange("exerciseId", r.ExerciseId, 1, int.MaxValue)
         .IntRange("sets", r.Sets, 1, 20)
         .IntRange("reps", r.Reps, 1, 100)
         .Optional("notes", r.Notes, 500);
        v.ThrowIfInvalid();
    }

    // ---------------- admin editing: nutrition ----------------

    public static void Validate(UpdateNutritionTargetsRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.Check(r.DailyCalories is not null || r.ProteinG is not null || r.CarbsG is not null || r.FatG is not null,
                "request", "Provide at least one field to change.")
         .IntRange("dailyCalories", r.DailyCalories, MinDailyCalories, 6000)
         .IntRange("proteinG", r.ProteinG, 0, 500)
         .IntRange("carbsG", r.CarbsG, 0, 1000)
         .IntRange("fatG", r.FatG, 0, 400);
        v.ThrowIfInvalid();
    }

    public static void Validate(AddNutritionPlanItemRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.IntRange("foodId", r.FoodId, 1, int.MaxValue, required: true)
         .IntRange("dayNumber", r.DayNumber, 1, 7, required: true)
         .OneOf("mealType", r.MealType, AllowedValues.MealTypeValues, required: true)
         .IntRange("quantityGrams", r.QuantityGrams, 1, 2000, required: true);
        v.ThrowIfInvalid();
    }

    public static void Validate(UpdateNutritionPlanItemRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.Check(r.FoodId is not null || r.DayNumber is not null || r.MealType is not null || r.QuantityGrams is not null,
                "request", "Provide at least one field to change.")
         .IntRange("foodId", r.FoodId, 1, int.MaxValue)
         .IntRange("dayNumber", r.DayNumber, 1, 7)
         .OneOf("mealType", r.MealType, AllowedValues.MealTypeValues)
         .IntRange("quantityGrams", r.QuantityGrams, 1, 2000);
        v.ThrowIfInvalid();
    }
}
