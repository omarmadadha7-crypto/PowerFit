namespace Powerfit.Business.DTOs.Plans;

// All requests below are used by admins (IPlanEditingService).
// For "Update*" requests a null value means "leave unchanged".

public record UpdateExercisePlanRequest
{
    public int? DaysPerWeek { get; init; }

    /// <summary>Empty string clears the notes.</summary>
    public string? Notes { get; init; }
}

public record AddExercisePlanItemRequest
{
    public int ExerciseId { get; init; }
    public int DayNumber { get; init; }

    /// <summary>Null = append at the end of the day.</summary>
    public int? OrderIndex { get; init; }

    public int Sets { get; init; }
    public int Reps { get; init; }
    public string? Notes { get; init; }

    /// <summary>Add the exercise even if it is contraindicated for the user's injuries.</summary>
    public bool OverrideSafetyCheck { get; init; }
}

public record UpdateExercisePlanItemRequest
{
    public int? ExerciseId { get; init; }
    public int? Sets { get; init; }
    public int? Reps { get; init; }

    /// <summary>Empty string clears the notes.</summary>
    public string? Notes { get; init; }

    /// <summary>Only used when ExerciseId changes: allow an exercise that is contraindicated for the user's injuries.</summary>
    public bool OverrideSafetyCheck { get; init; }
}

public record UpdateNutritionTargetsRequest
{
    public int? DailyCalories { get; init; }
    public int? ProteinG { get; init; }
    public int? CarbsG { get; init; }
    public int? FatG { get; init; }
}

public record AddNutritionPlanItemRequest
{
    public int FoodId { get; init; }
    public int DayNumber { get; init; }

    /// <summary>Breakfast | Lunch | Dinner | Snack.</summary>
    public string MealType { get; init; } = string.Empty;

    public int QuantityGrams { get; init; }

    /// <summary>Add the food even if it does not match the user's dietary restrictions / allergies.</summary>
    public bool OverrideSafetyCheck { get; init; }
}

public record UpdateNutritionPlanItemRequest
{
    public int? FoodId { get; init; }
    public int? DayNumber { get; init; }
    public string? MealType { get; init; }
    public int? QuantityGrams { get; init; }

    /// <summary>Only used when FoodId changes: allow a food that does not match the user's restrictions / allergies.</summary>
    public bool OverrideSafetyCheck { get; init; }
}
