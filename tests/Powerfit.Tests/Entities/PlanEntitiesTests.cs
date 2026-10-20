using DataAccess.Entities;
using Xunit;

namespace Powerfit.Tests.Entities;

public class PlanEntitiesTests
{
    [Fact]
    public void Macro_calories_are_4_4_9()
    {
        var plan = new NutritionPlan { DailyCalories = 2000, ProteinG = 150, CarbsG = 200, FatG = 70 };

        Assert.Equal(2030, plan.MacroCalories);
        Assert.True(plan.IsMacroConsistent());
        Assert.False(new NutritionPlan { DailyCalories = 2000, ProteinG = 50, CarbsG = 50, FatG = 10 }.IsMacroConsistent());
        Assert.False(new NutritionPlan { DailyCalories = 0 }.IsMacroConsistent());
    }

    [Fact]
    public void Day_totals_are_calculated_from_the_food_table()
    {
        var food = new Food { CaloriesPer100g = 100m, ProteinPer100g = 10m, CarbsPer100g = 20m, FatPer100g = 5m };
        var plan = new NutritionPlan();
        plan.NutritionPlanItems.Add(new NutritionPlanItem { DayNumber = 1, QuantityGrams = 150, Food = food });
        plan.NutritionPlanItems.Add(new NutritionPlanItem { DayNumber = 2, QuantityGrams = 100, Food = food });

        var day1 = plan.GetDayTotals(1);

        Assert.Equal(150m, day1.Calories);
        Assert.Equal(15m, day1.ProteinG);
        Assert.Equal(30m, day1.CarbsG);
        Assert.Equal(7.5m, day1.FatG);
        Assert.Equal(2, plan.GetWeeklyTotals().Count);
    }

    [Fact]
    public void Item_totals_need_the_food_to_be_loaded()
    {
        Assert.Throws<InvalidOperationException>(() => new NutritionPlanItem { QuantityGrams = 100 }.GetTotals());
    }

    [Fact]
    public void Plan_status_helpers()
    {
        var plan = new FitnessPlan();
        Assert.True(plan.IsDraft);

        plan.Activate();
        Assert.True(plan.IsCurrentlyActive);

        plan.Archive();
        Assert.True(plan.IsArchived);
    }

    [Fact]
    public void Exercise_plan_counts_distinct_days_and_next_order()
    {
        var plan = new ExercisePlan { DaysPerWeek = 2 };
        plan.ExercisePlanItems.Add(new ExercisePlanItem { DayNumber = 1, OrderIndex = 1 });
        plan.ExercisePlanItems.Add(new ExercisePlanItem { DayNumber = 1, OrderIndex = 2 });
        plan.ExercisePlanItems.Add(new ExercisePlanItem { DayNumber = 4, OrderIndex = 1 });

        Assert.Equal(2, plan.ScheduledDays);
        Assert.True(plan.IsScheduleValid());
        Assert.Equal(3, plan.GetNextOrderIndex(1));
        Assert.Equal(1, plan.GetNextOrderIndex(6));
    }

    [Fact]
    public void Ai_log_validation_follows_the_sql_checks()
    {
        Assert.True(AigenerationLog.IsValidJson("{\"a\":1}"));
        Assert.False(AigenerationLog.IsValidJson("42"));
        Assert.False(AigenerationLog.IsValidJson("not json"));

        Assert.Throws<ArgumentException>(() =>
            AigenerationLog.Create(1, "model", "{}", null, GenerationStatuses.Success));   // Success needs a response
        var log = AigenerationLog.Create(1, "model", "{}", "{}", GenerationStatuses.Success);
        Assert.True(log.IsSuccess);
    }
}
