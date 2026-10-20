namespace Powerfit.Business.DTOs.Exercises;

public record ExerciseDto(
    int ExerciseId,
    string Name,
    string MuscleGroup,
    string ExerciseType,
    string Equipment,
    string DifficultyLevel,
    string? ContraindicatedAreas,
    bool IsActive);
