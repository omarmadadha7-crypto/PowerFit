namespace Powerfit.Business.DTOs.Exercises;

public record ExerciseQuery
{
    public string? Term { get; init; }
    public string? MuscleGroup { get; init; }
    public string? ExerciseType { get; init; }
    public string? DifficultyLevel { get; init; }
    public string? Equipment { get; init; }

    /// <summary>Null = both active and inactive.</summary>
    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
