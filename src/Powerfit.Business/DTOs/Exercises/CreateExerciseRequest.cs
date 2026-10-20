namespace Powerfit.Business.DTOs.Exercises;

public record CreateExerciseRequest
{
    public string Name { get; init; } = string.Empty;
    public string MuscleGroup { get; init; } = string.Empty;

    /// <summary>Strength | Cardio | Flexibility | Balance.</summary>
    public string ExerciseType { get; init; } = string.Empty;

    /// <summary>"None" for bodyweight exercises.</summary>
    public string Equipment { get; init; } = "None";

    /// <summary>Beginner | Intermediate | Advanced.</summary>
    public string DifficultyLevel { get; init; } = string.Empty;

    /// <summary>Comma separated body areas, e.g. "knee, lower back".</summary>
    public string? ContraindicatedAreas { get; init; }

    public bool IsActive { get; init; } = true;
}
