using DataAccess.Entities;
using Xunit;

namespace Powerfit.Tests.Entities;

public class ExerciseSafetyTests
{
    private static Exercise Exercise(string? contraindicated) => new()
    {
        Name = "Squat",
        MuscleGroup = "Legs",
        ExerciseType = ExerciseTypes.Strength,
        DifficultyLevel = DifficultyLevels.Beginner,
        ContraindicatedAreas = contraindicated
    };

    [Fact]
    public void No_injuries_or_no_contraindications_is_safe()
    {
        Assert.True(Exercise("knee").IsSafeFor(null));
        Assert.True(Exercise("knee").IsSafeFor([]));
        Assert.True(Exercise(null).IsSafeFor(["knee"]));
    }

    [Fact]
    public void Arabic_injury_matches_english_contraindication()
    {
        Assert.False(Exercise("knee, lower back").IsSafeFor(["الركبة"]));
    }

    [Fact]
    public void English_injury_matches_arabic_contraindication()
    {
        Assert.False(Exercise("الركبة").IsSafeFor(["knee"]));
    }

    [Fact]
    public void Different_area_is_safe()
    {
        Assert.True(Exercise("shoulder").IsSafeFor(["knee"]));
    }

    [Fact]
    public void Plain_back_injury_blocks_lower_back_exercises()
    {
        Assert.False(Exercise("lower back").IsSafeFor(["back pain"]));
    }

    [Fact]
    public void Lower_back_injury_does_not_block_upper_back_exercises()
    {
        Assert.True(Exercise("upper back").IsSafeFor(["lower back"]));
    }

    [Fact]
    public void Unknown_terms_still_match_as_plain_text()
    {
        Assert.False(Exercise("pregnancy").IsSafeFor(["Pregnancy"]));
    }

    [Fact]
    public void Blank_injuries_are_ignored()
    {
        Assert.True(Exercise("knee").IsSafeFor(["", "  "]));
    }
}
