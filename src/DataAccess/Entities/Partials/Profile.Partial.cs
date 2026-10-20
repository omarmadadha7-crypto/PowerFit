using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DataAccess.Entities;

public partial class Profile
{
    /// <summary>Same rule as CK_Profile_Completed in SQL.</summary>
    [NotMapped]
    public bool IsComplete =>
        Gender is not null
        && HeightCm is not null
        && WeightKg is not null
        && Goal is not null
        && ActivityLevel is not null
        && ExperienceLevel is not null
        && TrainingLocation is not null
        && PreferredDaysPerWeek is not null;

    [NotMapped]
    public decimal? Bmi
    {
        get
        {
            if (HeightCm is not { } h || h <= 0 || WeightKg is not { } w || w <= 0) return null;
            var meters = h / 100m;
            return Math.Round(w / (meters * meters), 1);
        }
    }

    [NotMapped]
    public string? BmiCategory => Bmi switch
    {
        null => null,
        < 18.5m => "Underweight",
        < 25m => "Normal",
        < 30m => "Overweight",
        _ => "Obese"
    };

    [NotMapped] public IReadOnlyList<string> InjuryList => TextList.Split(Injuries);
    [NotMapped] public IReadOnlyList<string> DietaryRestrictionList => TextList.Split(DietaryRestrictions);

    /// <summary>Keeps ProfileCompleted consistent with the data (otherwise SQL rejects the row).</summary>
    public void RefreshCompletion() => ProfileCompleted = IsComplete;

    /// <summary>Copies every editable field (not UserId) and refreshes ProfileCompleted.</summary>
    public void CopyFrom(Profile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Gender = source.Gender;
        HeightCm = source.HeightCm;
        WeightKg = source.WeightKg;
        Goal = source.Goal;
        ActivityLevel = source.ActivityLevel;
        ExperienceLevel = source.ExperienceLevel;
        TrainingLocation = source.TrainingLocation;
        PreferredDaysPerWeek = source.PreferredDaysPerWeek;
        Injuries = source.Injuries;
        DietaryRestrictions = source.DietaryRestrictions;

        RefreshCompletion();
    }

    /// <summary>Mifflin-St Jeor equation. Age comes from Person.Age.</summary>
    public decimal? CalculateBmr(int age)
    {
        if (HeightCm is not { } h || WeightKg is not { } w || age <= 0) return null;

        var value = 10m * w + 6.25m * h - 5m * age;
        return Gender switch
        {
            Genders.Male => value + 5m,
            Genders.Female => value - 161m,
            _ => value - 78m
        };
    }

    /// <summary>Total daily energy expenditure = BMR x activity multiplier.</summary>
    public decimal? CalculateTdee(int age) =>
        CalculateBmr(age) is { } bmr
            ? Math.Round(bmr * ActivityLevels.Multiplier(ActivityLevel), 0)
            : null;

    /// <summary>Reads the free-text DietaryRestrictions column (see <see cref="DietaryFlags.FromText"/>).</summary>
    public DietaryFlags GetDietaryFlags() => DietaryFlags.FromText(DietaryRestrictions);
}
