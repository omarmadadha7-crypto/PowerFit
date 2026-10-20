using System;

namespace Powerfit.Business.DTOs.Profiles;

public record ProfileDto(
    int UserId,
    string? Gender,
    int? HeightCm,
    decimal? WeightKg,
    string? Goal,
    string? ActivityLevel,
    string? ExperienceLevel,
    string? TrainingLocation,
    int? PreferredDaysPerWeek,
    string? Injuries,
    string? DietaryRestrictions,
    bool ProfileCompleted,
    DateOnly? BirthDate,
    int? Age,
    decimal? Bmi,
    string? BmiCategory,
    decimal? EstimatedDailyCalories);
