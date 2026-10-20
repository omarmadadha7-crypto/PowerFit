// Constants that mirror the SQL CHECK constraints (no magic strings).

using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.Entities;

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
}

public static class PlanStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Archived = "Archived";

    public static readonly string[] All = [Draft, Active, Archived];
}

public static class ItemSources
{
    public const string AI = "AI";
    public const string Admin = "Admin";
}

public static class MealTypes
{
    public const string Breakfast = "Breakfast";
    public const string Lunch = "Lunch";
    public const string Dinner = "Dinner";
    public const string Snack = "Snack";

    public static readonly string[] All = [Breakfast, Lunch, Dinner, Snack];

    /// <summary>Sort order inside a day (Breakfast, Lunch, Dinner, Snack).</summary>
    public static int Order(string? mealType) => mealType switch
    {
        Breakfast => 1,
        Lunch => 2,
        Dinner => 3,
        Snack => 4,
        _ => 99
    };
}

public static class Genders
{
    public const string Male = "Male";
    public const string Female = "Female";
    public const string Other = "Other";
}

public static class Goals
{
    public const string MuscleGain = "Muscle Gain";
    public const string FatLoss = "Fat Loss";
    public const string Strength = "Strength";
    public const string Fitness = "Fitness";
}

public static class ActivityLevels
{
    public const string Sedentary = "Sedentary";
    public const string Light = "Light";
    public const string Moderate = "Moderate";
    public const string Active = "Active";
    public const string VeryActive = "Very Active";

    /// <summary>Standard TDEE activity multipliers.</summary>
    public static decimal Multiplier(string? level) => level switch
    {
        Sedentary => 1.2m,
        Light => 1.375m,
        Moderate => 1.55m,
        Active => 1.725m,
        VeryActive => 1.9m,
        _ => 1.2m
    };
}

public static class DifficultyLevels
{
    public const string Beginner = "Beginner";
    public const string Intermediate = "Intermediate";
    public const string Advanced = "Advanced";

    public static readonly string[] All = [Beginner, Intermediate, Advanced];

    /// <summary>1 = Beginner, 2 = Intermediate, 3 = Advanced, 0 = unknown.</summary>
    public static int Rank(string? level) => level switch
    {
        Beginner => 1,
        Intermediate => 2,
        Advanced => 3,
        _ => 0
    };

    /// <summary>All levels up to and including <paramref name="max"/>. Unknown/null => all levels.</summary>
    public static string[] UpTo(string? max)
    {
        var rank = Rank(max);
        return rank == 0 ? [.. All] : All.Where(l => Rank(l) <= rank).ToArray();
    }
}

public static class ExperienceLevels
{
    public const string Beginner = DifficultyLevels.Beginner;
    public const string Intermediate = DifficultyLevels.Intermediate;
    public const string Advanced = DifficultyLevels.Advanced;
}

public static class TrainingLocations
{
    public const string Gym = "Gym";
    public const string Home = "Home";
    public const string Outdoor = "Outdoor";
}

public static class ExerciseTypes
{
    public const string Strength = "Strength";
    public const string Cardio = "Cardio";
    public const string Flexibility = "Flexibility";
    public const string Balance = "Balance";
}

public static class GenerationStatuses
{
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Timeout = "Timeout";
}

/// <summary>
/// Allergens that have NO column of their own in Food (gluten, dairy and nuts have one).
/// Stored in Food.Allergens as a comma separated list without spaces, e.g. "Egg,Sesame".
/// </summary>
public static class FoodAllergens
{
    public const string Egg = "Egg";
    public const string Soy = "Soy";
    public const string Fish = "Fish";
    public const string Shellfish = "Shellfish";
    public const string Sesame = "Sesame";

    public static readonly string[] All = [Egg, Soy, Fish, Shellfish, Sesame];

    /// <summary>Values that are not in <see cref="All"/> (case-insensitive).</summary>
    public static IReadOnlyList<string> Unknown(string? text) => TextList.Split(text)
        .Where(t => !All.Contains(t, StringComparer.OrdinalIgnoreCase))
        .ToList();

    /// <summary>Canonical casing, distinct, "A,B" without spaces. Null when empty.</summary>
    public static string? Normalize(string? text)
    {
        var tokens = TextList.Split(text)
            .Select(t => All.FirstOrDefault(a => string.Equals(a, t, StringComparison.OrdinalIgnoreCase)) ?? t)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return tokens.Length == 0 ? null : string.Join(",", tokens);
    }
}
