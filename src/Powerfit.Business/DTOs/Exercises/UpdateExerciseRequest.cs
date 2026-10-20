namespace Powerfit.Business.DTOs.Exercises;

/// <summary>Full replace (PUT semantics).</summary>
public record UpdateExerciseRequest
{
    public string Name { get; init; } = string.Empty;
    public string MuscleGroup { get; init; } = string.Empty;
    public string ExerciseType { get; init; } = string.Empty;
    public string Equipment { get; init; } = "None";
    public string DifficultyLevel { get; init; } = string.Empty;
    public string? ContraindicatedAreas { get; init; }
    public bool IsActive { get; init; } = true;
}
