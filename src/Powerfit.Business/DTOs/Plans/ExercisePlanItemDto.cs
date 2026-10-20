namespace Powerfit.Business.DTOs.Plans;

public record ExercisePlanItemDto(
    int ItemId,
    int ExerciseId,
    string ExerciseName,
    string MuscleGroup,
    string ExerciseType,
    string Equipment,
    int DayNumber,
    int OrderIndex,
    int Sets,
    int Reps,
    string Source,
    string? Notes);
