using Powerfit.Business.AI;
using Powerfit.Business.Validators;
using Xunit;

namespace Powerfit.Tests.Business;

public class AiPlanValidatorTests
{
    private static AiPlanRequest Request(int daysPerWeek = 2) => new(
        new AiProfileSnapshot(30, "Male", 180, 80m, 24.7m, "Fitness", "Moderate", "Beginner", "Gym", [], [], 2500m),
        daysPerWeek,
        "en",
        null,
        [
            new AiExerciseOption(1, "Squat", "Legs", "Strength", "None", "Beginner"),
            new AiExerciseOption(2, "Push-up", "Chest", "Strength", "None", "Beginner")
        ],
        [new AiFoodOption(1, "Rice", "Grain", null, 100m, 10m, 10m, 0m)]);

    private static AiPlanDraft ValidDraft(decimal dailyCalories = 1500m, decimal protein = 100m, decimal carbs = 275m, int grams = 1500) => new()
    {
        ExercisePlan = new AiExercisePlanDraft
        {
            Items =
            [
                new AiExerciseItemDraft { DayNumber = 1, OrderIndex = 1, ExerciseId = 1, Sets = 3, Reps = 10 },
                new AiExerciseItemDraft { DayNumber = 3, OrderIndex = 1, ExerciseId = 2, Sets = 3, Reps = 12 }
            ]
        },
        NutritionPlan = new AiNutritionPlanDraft
        {
            DailyCalories = dailyCalories,
            ProteinG = protein,
            CarbsG = carbs,
            FatG = 0,
            Items = Enumerable.Range(1, 7)
                .Select(d => new AiNutritionItemDraft { DayNumber = d, MealType = "Breakfast", FoodId = 1, QuantityGrams = grams })
                .ToList()
        }
    };

    [Fact]
    public void A_consistent_plan_has_no_errors()
    {
        Assert.Empty(AiPlanValidator.Validate(ValidDraft(), Request()));
    }

    [Fact]
    public void Missing_parts_are_reported()
    {
        var errors = AiPlanValidator.Validate(new AiPlanDraft(), Request());

        Assert.Contains(errors, e => e.Contains("exercisePlan is missing"));
        Assert.Contains(errors, e => e.Contains("nutritionPlan is missing"));
        Assert.NotEmpty(AiPlanValidator.Validate(null, Request()));
    }

    [Fact]
    public void An_exercise_outside_the_catalog_is_rejected()
    {
        var draft = ValidDraft();
        draft.ExercisePlan!.Items[0].ExerciseId = 99;

        Assert.Contains(AiPlanValidator.Validate(draft, Request()), e => e.Contains("exerciseId 99"));
    }

    [Fact]
    public void The_number_of_training_days_must_match()
    {
        Assert.Contains(AiPlanValidator.Validate(ValidDraft(), Request(daysPerWeek: 3)), e => e.Contains("exactly 3 training days"));
    }

    [Fact]
    public void A_missing_nutrition_day_is_reported()
    {
        var draft = ValidDraft();
        draft.NutritionPlan!.Items.RemoveAll(i => i.DayNumber == 7);

        Assert.Contains(AiPlanValidator.Validate(draft, Request()), e => e.Contains("missing day(s): 7"));
    }

    [Fact]
    public void Calories_below_the_minimum_are_rejected()
    {
        // 1000 kcal: macros (100 g protein + 150 g carbs) and the real daily intake both match, only the floor fails.
        var draft = ValidDraft(dailyCalories: 1000m, protein: 100m, carbs: 150m, grams: 1000);

        var errors = AiPlanValidator.Validate(draft, Request());

        Assert.Contains(errors, e => e.Contains("dailyCalories must be between"));
        Assert.Equal(1200, PlanValidator.MinDailyCalories);
    }

    [Fact]
    public void Macros_that_do_not_add_up_are_rejected()
    {
        var draft = ValidDraft(dailyCalories: 2500m);

        Assert.Contains(AiPlanValidator.Validate(draft, Request()), e => e.Contains("macros give"));
    }

    [Fact]
    public void A_day_far_from_the_calorie_target_is_rejected()
    {
        var draft = ValidDraft();
        draft.NutritionPlan!.Items.First(i => i.DayNumber == 2).QuantityGrams = 500;   // ~500 kcal instead of 1500

        Assert.Contains(AiPlanValidator.Validate(draft, Request()), e => e.Contains("day 2"));
    }

    [Fact]
    public void Normalize_fixes_meal_type_casing_and_null_lists()
    {
        var draft = ValidDraft();
        draft.NutritionPlan!.Items[0].MealType = " breakfast ";

        AiPlanValidator.Normalize(draft);

        Assert.Equal("Breakfast", draft.NutritionPlan.Items[0].MealType);
    }
}
