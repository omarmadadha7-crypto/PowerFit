using System;

namespace Powerfit.Business.DTOs.Profiles;

/// <summary>
/// Full replace (PUT semantics): every field you leave null is cleared.
/// Allowed values: Gender = Male|Female|Other, Goal = Muscle Gain|Fat Loss|Strength|Fitness,
/// ActivityLevel = Sedentary|Light|Moderate|Active|Very Active,
/// ExperienceLevel = Beginner|Intermediate|Advanced, TrainingLocation = Gym|Home|Outdoor.
/// </summary>
public record UpdateProfileRequest
{
    public string? Gender { get; init; }
    public int? HeightCm { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Goal { get; init; }
    public string? ActivityLevel { get; init; }
    public string? ExperienceLevel { get; init; }
    public string? TrainingLocation { get; init; }
    public int? PreferredDaysPerWeek { get; init; }
    public string? Injuries { get; init; }
    public string? DietaryRestrictions { get; init; }

    /// <summary>Stored on Person (used to calculate age / calorie estimates).</summary>
    public DateOnly? BirthDate { get; init; }
}
