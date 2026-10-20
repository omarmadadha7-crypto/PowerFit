using System;
using DataAccess.Entities;
using Powerfit.Business.DTOs.Profiles;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Mapping;

public static class ProfileMappings
{
    /// <summary>Person is optional but gives BirthDate / Age / calorie estimate.</summary>
    public static ProfileDto ToDto(this Profile profile, Person? person)
    {
        var age = person?.Age;
        decimal? tdee = age is { } a ? profile.CalculateTdee(a) : null;

        return new ProfileDto(
            profile.UserId,
            profile.Gender,
            profile.HeightCm,
            profile.WeightKg,
            profile.Goal,
            profile.ActivityLevel,
            profile.ExperienceLevel,
            profile.TrainingLocation,
            profile.PreferredDaysPerWeek,
            profile.Injuries,
            profile.DietaryRestrictions,
            profile.ProfileCompleted,
            person?.BirthDate,
            age,
            profile.Bmi,
            profile.BmiCategory,
            tdee);
    }

    /// <summary>Returned for users who have not saved a profile yet.</summary>
    public static ProfileDto EmptyProfile(int userId, Person? person) =>
        new(userId, null, null, null, null, null, null, null, null, null, null,
            false, person?.BirthDate, person?.Age, null, null, null);

    /// <summary>Builds a detached Profile with cleaned / canonical values (UserId is set by the repository).</summary>
    public static Profile ToEntity(this UpdateProfileRequest r) => new()
    {
        Gender = MappingHelpers.CanonicalOrNull(r.Gender, AllowedValues.GenderValues),
        HeightCm = r.HeightCm,
        WeightKg = r.WeightKg is { } w ? Math.Round(w, 2) : (decimal?)null,
        Goal = MappingHelpers.CanonicalOrNull(r.Goal, AllowedValues.GoalValues),
        ActivityLevel = MappingHelpers.CanonicalOrNull(r.ActivityLevel, AllowedValues.ActivityLevelValues),
        ExperienceLevel = MappingHelpers.CanonicalOrNull(r.ExperienceLevel, AllowedValues.LevelValues),
        TrainingLocation = MappingHelpers.CanonicalOrNull(r.TrainingLocation, AllowedValues.TrainingLocationValues),
        PreferredDaysPerWeek = r.PreferredDaysPerWeek,
        Injuries = MappingHelpers.Clean(r.Injuries),
        DietaryRestrictions = MappingHelpers.Clean(r.DietaryRestrictions)
    };
}
