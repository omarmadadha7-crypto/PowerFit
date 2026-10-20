using System.Collections.Generic;
using DataAccess.Entities;

namespace Powerfit.Business.AI;

// ============================================================================
//  Input sent to the AI
// ============================================================================

public sealed record AiProfileSnapshot(
    int? Age,
    string Gender,
    int HeightCm,
    decimal WeightKg,
    decimal? Bmi,
    string Goal,
    string ActivityLevel,
    string ExperienceLevel,
    string TrainingLocation,
    IReadOnlyList<string> Injuries,
    IReadOnlyList<string> DietaryRestrictions,
    decimal? EstimatedTdee);

/// <summary>One exercise the AI is allowed to use.</summary>
public sealed record AiExerciseOption(
    int Id,
    string Name,
    string MuscleGroup,
    string Type,
    string Equipment,
    string Difficulty);

/// <summary>One food the AI is allowed to use. Macros are per 100 g.</summary>
public sealed record AiFoodOption(
    int Id,
    string Name,
    string Category,
    string? Meals,
    decimal Kcal,
    decimal Protein,
    decimal Carbs,
    decimal Fat);

public sealed record AiPlanRequest(
    AiProfileSnapshot Profile,
    int DaysPerWeek,
    string Language,
    string? AdditionalNotes,
    IReadOnlyList<AiExerciseOption> Exercises,
    IReadOnlyList<AiFoodOption> Foods);

// ============================================================================
//  Output expected from the AI (deserialized from its JSON answer)
// ============================================================================

public sealed class AiPlanDraft
{
    public AiExercisePlanDraft? ExercisePlan { get; set; }
    public AiNutritionPlanDraft? NutritionPlan { get; set; }
}

public sealed class AiExercisePlanDraft
{
    public string? Notes { get; set; }
    public List<AiExerciseItemDraft> Items { get; set; } = new();
}

public sealed class AiExerciseItemDraft
{
    public int DayNumber { get; set; }
    public int OrderIndex { get; set; }
    public int ExerciseId { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
}

public sealed class AiNutritionPlanDraft
{
    public decimal DailyCalories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public List<AiNutritionItemDraft> Items { get; set; } = new();
}

public sealed class AiNutritionItemDraft
{
    public int DayNumber { get; set; }
    public string MealType { get; set; } = string.Empty;
    public int FoodId { get; set; }
    public decimal QuantityGrams { get; set; }
}

// ============================================================================
//  Result of one generation attempt (what gets stored in AIGenerationLog)
// ============================================================================

/// <param name="Status">Success | Failed | Timeout (see GenerationStatuses).</param>
/// <param name="Plan">Validated plan. Only set when Status is Success.</param>
public sealed record AiGenerationOutcome(
    string Status,
    string Model,
    string RequestJson,
    string? ResponseJson,
    AiPlanDraft? Plan,
    string? Error)
{
    public bool Succeeded => Status == GenerationStatuses.Success && Plan is not null;
}
