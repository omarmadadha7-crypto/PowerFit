using DataAccess.Entities;

namespace Powerfit.Business.Validators;

/// <summary>The allowed values of every "enum-like" column (same lists as the SQL CHECK constraints).</summary>
public static class AllowedValues
{
    public static readonly string[] GenderValues = [Genders.Male, Genders.Female, Genders.Other];

    public static readonly string[] GoalValues = [Goals.MuscleGain, Goals.FatLoss, Goals.Strength, Goals.Fitness];

    public static readonly string[] ActivityLevelValues =
        [ActivityLevels.Sedentary, ActivityLevels.Light, ActivityLevels.Moderate, ActivityLevels.Active, ActivityLevels.VeryActive];

    public static readonly string[] TrainingLocationValues =
        [TrainingLocations.Gym, TrainingLocations.Home, TrainingLocations.Outdoor];

    public static readonly string[] ExerciseTypeValues =
        [ExerciseTypes.Strength, ExerciseTypes.Cardio, ExerciseTypes.Flexibility, ExerciseTypes.Balance];

    // Beginner | Intermediate | Advanced (used by exercises AND by Profile.ExperienceLevel)
    public static readonly string[] LevelValues = DifficultyLevels.All;

    public static readonly string[] MealTypeValues = MealTypes.All;

    public static readonly string[] PlanStatusValues = PlanStatuses.All;
}
