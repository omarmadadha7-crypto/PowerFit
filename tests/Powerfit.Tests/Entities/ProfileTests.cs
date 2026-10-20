using DataAccess.Entities;
using Xunit;

namespace Powerfit.Tests.Entities;

public class ProfileTests
{
    [Fact]
    public void Bmi_and_category()
    {
        var profile = new Profile { HeightCm = 180, WeightKg = 80m };

        Assert.Equal(24.7m, profile.Bmi);
        Assert.Equal("Normal", profile.BmiCategory);
        Assert.Equal("Underweight", new Profile { HeightCm = 180, WeightKg = 50m }.BmiCategory);
        Assert.Equal("Obese", new Profile { HeightCm = 170, WeightKg = 110m }.BmiCategory);
    }

    [Fact]
    public void Bmi_is_null_without_height_or_weight()
    {
        Assert.Null(new Profile { WeightKg = 80m }.Bmi);
        Assert.Null(new Profile { HeightCm = 180 }.BmiCategory);
    }

    [Fact]
    public void Tdee_uses_mifflin_st_jeor_and_the_activity_multiplier()
    {
        var man = new Profile { Gender = Genders.Male, HeightCm = 180, WeightKg = 80m, ActivityLevel = ActivityLevels.Moderate };
        var woman = new Profile { Gender = Genders.Female, HeightCm = 165, WeightKg = 60m, ActivityLevel = ActivityLevels.Sedentary };

        Assert.Equal(2759m, man.CalculateTdee(30));     // (800 + 1125 - 150 + 5) x 1.55
        Assert.Equal(1614m, woman.CalculateTdee(25));   // (600 + 1031.25 - 125 - 161) x 1.2
        Assert.Null(man.CalculateTdee(0));
    }

    [Fact]
    public void Completion_follows_the_data()
    {
        var profile = new Profile
        {
            Gender = Genders.Male, HeightCm = 180, WeightKg = 80m, Goal = Goals.Fitness,
            ActivityLevel = ActivityLevels.Light, ExperienceLevel = DifficultyLevels.Beginner,
            TrainingLocation = TrainingLocations.Gym
        };

        profile.RefreshCompletion();
        Assert.False(profile.ProfileCompleted);          // PreferredDaysPerWeek is missing

        profile.PreferredDaysPerWeek = 3;
        profile.RefreshCompletion();
        Assert.True(profile.ProfileCompleted);
    }

    [Fact]
    public void Injury_list_splits_and_removes_duplicates()
    {
        var profile = new Profile { Injuries = "knee, lower back; neck، Knee" };

        Assert.Equal(new[] { "knee", "lower back", "neck" }, profile.InjuryList.ToArray());
    }

    [Fact]
    public void Dietary_flags_come_from_the_text_column()
    {
        var profile = new Profile { DietaryRestrictions = "vegetarian, egg allergy" };

        var flags = profile.GetDietaryFlags();

        Assert.True(flags.Vegetarian);
        Assert.Contains(FoodAllergens.Egg, flags.ExcludedAllergens!);
    }
}
