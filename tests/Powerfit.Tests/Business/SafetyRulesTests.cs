using DataAccess.Entities;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Validators;
using Xunit;

namespace Powerfit.Tests.Business;

public class SafetyRulesTests
{
    private static (Profile Profile, Person Person) Make(
        int? age, string goal = Goals.Fitness, decimal weightKg = 75m, int heightCm = 175)
    {
        var person = new Person
        {
            BirthDate = age is { } a
                ? DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-a).AddDays(-1)
                : null
        };
        var profile = new Profile { Goal = goal, WeightKg = weightKg, HeightCm = heightCm };
        return (profile, person);
    }

    private static string[] ErrorKeys(ValidationException ex) => ex.Errors.Keys.ToArray();

    [Fact]
    public void Birth_date_is_required()
    {
        var (profile, person) = Make(null);

        var ex = Assert.Throws<ValidationException>(() => SafetyRules.EnsureEligibleForPlan(profile, person));

        Assert.Contains("birthDate", ErrorKeys(ex));
    }

    [Fact]
    public void Below_minimum_age_is_refused()
    {
        var (profile, person) = Make(SafetyRules.MinPlanAge - 1);

        var ex = Assert.Throws<ValidationException>(() => SafetyRules.EnsureEligibleForPlan(profile, person));

        Assert.Contains("birthDate", ErrorKeys(ex));
    }

    [Fact]
    public void Minimum_age_gets_a_plan_and_the_age_is_returned()
    {
        var (profile, person) = Make(SafetyRules.MinPlanAge);

        Assert.Equal(SafetyRules.MinPlanAge, SafetyRules.EnsureEligibleForPlan(profile, person));
    }

    [Fact]
    public void Fat_loss_is_refused_for_minors()
    {
        var (profile, person) = Make(17, Goals.FatLoss);

        var ex = Assert.Throws<ValidationException>(() => SafetyRules.EnsureEligibleForPlan(profile, person));

        Assert.Contains("goal", ErrorKeys(ex));
    }

    [Fact]
    public void Fat_loss_is_refused_when_underweight()
    {
        var (profile, person) = Make(25, Goals.FatLoss, weightKg: 50m, heightCm: 180);   // BMI 15.4

        var ex = Assert.Throws<ValidationException>(() => SafetyRules.EnsureEligibleForPlan(profile, person));

        Assert.Contains("goal", ErrorKeys(ex));
    }

    [Fact]
    public void Fat_loss_is_allowed_for_a_normal_adult()
    {
        var (profile, person) = Make(25, Goals.FatLoss, weightKg: 90m, heightCm: 175);

        Assert.Equal(25, SafetyRules.EnsureEligibleForPlan(profile, person));
    }

    [Fact]
    public void Other_goals_are_allowed_when_underweight()
    {
        var (profile, person) = Make(25, Goals.MuscleGain, weightKg: 50m, heightCm: 180);

        Assert.Equal(25, SafetyRules.EnsureEligibleForPlan(profile, person));
    }
}
