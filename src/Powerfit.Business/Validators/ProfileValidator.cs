using System;
using Powerfit.Business.DTOs.Profiles;

namespace Powerfit.Business.Validators;

public static class ProfileValidator
{
    public static void Validate(UpdateProfileRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var v = new ValidationBuilder();
        v.OneOf("gender", r.Gender, AllowedValues.GenderValues)
         .IntRange("heightCm", r.HeightCm, 50, 260)
         .DecimalRange("weightKg", r.WeightKg, 20m, 500m)
         .OneOf("goal", r.Goal, AllowedValues.GoalValues)
         .OneOf("activityLevel", r.ActivityLevel, AllowedValues.ActivityLevelValues)
         .OneOf("experienceLevel", r.ExperienceLevel, AllowedValues.LevelValues)
         .OneOf("trainingLocation", r.TrainingLocation, AllowedValues.TrainingLocationValues)
         .IntRange("preferredDaysPerWeek", r.PreferredDaysPerWeek, 1, 7)
         .Optional("injuries", r.Injuries, 500)
         .Optional("dietaryRestrictions", r.DietaryRestrictions, 500);

        AuthValidator.ValidateBirthDate(v, r.BirthDate);
        v.ThrowIfInvalid();
    }
}
