using Powerfit.Business.DTOs.Auth;
using Powerfit.Business.DTOs.Plans;
using Powerfit.Business.DTOs.Profiles;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Validators;
using Xunit;

namespace Powerfit.Tests.Business;

public class ValidatorTests
{
    private static string[] Keys(ValidationException ex) => ex.Errors.Keys.ToArray();

    // ---------------- plans ----------------

    [Fact]
    public void Admin_cannot_set_calories_below_the_floor()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            PlanValidator.Validate(new UpdateNutritionTargetsRequest { DailyCalories = PlanValidator.MinDailyCalories - 1 }));

        Assert.Contains("dailyCalories", Keys(ex));
        PlanValidator.Validate(new UpdateNutritionTargetsRequest { DailyCalories = PlanValidator.MinDailyCalories });
    }

    [Fact]
    public void Updates_need_at_least_one_field()
    {
        Assert.Throws<ValidationException>(() => PlanValidator.Validate(new UpdateExercisePlanItemRequest()));
        Assert.Throws<ValidationException>(() => PlanValidator.Validate(new UpdateNutritionPlanItemRequest()));

        // the safety override alone is not a change
        Assert.Throws<ValidationException>(() =>
            PlanValidator.Validate(new UpdateExercisePlanItemRequest { OverrideSafetyCheck = true }));
    }

    [Fact]
    public void Add_exercise_item_ranges_are_checked()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            PlanValidator.Validate(new AddExercisePlanItemRequest { ExerciseId = 1, DayNumber = 9, Sets = 3, Reps = 10 }));

        Assert.Contains("dayNumber", Keys(ex));
    }

    [Fact]
    public void Generate_request_days_are_checked()
    {
        PlanValidator.Validate(new GeneratePlanRequest { DaysPerWeek = 7 });
        Assert.Throws<ValidationException>(() => PlanValidator.Validate(new GeneratePlanRequest { DaysPerWeek = 8 }));
    }

    // ---------------- foods ----------------

    [Fact]
    public void Unknown_allergens_are_rejected()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            FoodValidator.Validate("Tofu", "Protein", null, 120m, 12m, 3m, 7m, "Soy, Pollen"));

        Assert.Contains("allergens", Keys(ex));
        FoodValidator.Validate("Tofu", "Protein", null, 120m, 12m, 3m, 7m, "soy");
        FoodValidator.Validate("Tofu", "Protein", null, 120m, 12m, 3m, 7m);
    }

    [Fact]
    public void Food_macros_cannot_exceed_100_grams()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            FoodValidator.Validate("Odd", "Misc", null, 400m, 60m, 60m, 10m));

        Assert.Contains("macros", Keys(ex));
    }

    // ---------------- auth / profile ----------------

    [Theory]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public void Weak_passwords_are_rejected(string password)
    {
        var request = new RegisterRequest { FirstName = "A", LastName = "B", Email = "a@b.com", Password = password };

        var ex = Assert.Throws<ValidationException>(() => AuthValidator.Validate(request));

        Assert.Contains("password", Keys(ex));
    }

    [Fact]
    public void A_valid_registration_passes()
    {
        AuthValidator.Validate(new RegisterRequest
        {
            FirstName = "Sara", LastName = "Ali", Email = "sara@example.com", Password = "Passw0rdOk",
            BirthDate = new DateOnly(1995, 5, 5)
        });
    }

    [Fact]
    public void A_bad_email_and_a_future_birth_date_are_rejected()
    {
        var request = new RegisterRequest
        {
            FirstName = "A", LastName = "B", Email = "not-an-email", Password = "Passw0rdOk",
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5)
        };

        var ex = Assert.Throws<ValidationException>(() => AuthValidator.Validate(request));

        Assert.Contains("email", Keys(ex));
        Assert.Contains("birthDate", Keys(ex));
    }

    [Fact]
    public void Profile_values_are_checked()
    {
        var ex = Assert.Throws<ValidationException>(() =>
            ProfileValidator.Validate(new UpdateProfileRequest { HeightCm = 20, Goal = "Fly", WeightKg = 70m }));

        Assert.Contains("heightCm", Keys(ex));
        Assert.Contains("goal", Keys(ex));
    }
}
