using DataAccess.Entities;
using Xunit;

namespace Powerfit.Tests.Entities;

public class FoodTests
{
    [Fact]
    public void Food_with_a_listed_allergen_is_not_compatible()
    {
        var food = new Food { Allergens = "Egg,Sesame" };

        Assert.False(food.IsCompatibleWith(new DietaryFlags(false, false, false, false, [FoodAllergens.Egg])));
        Assert.True(food.IsCompatibleWith(new DietaryFlags(false, false, false, false, [FoodAllergens.Fish])));
    }

    [Fact]
    public void Existing_flags_still_work()
    {
        var milk = new Food { ContainsDairy = true, IsVegetarian = true };

        Assert.False(milk.IsCompatibleWith(new DietaryFlags(false, false, true, false)));
        Assert.True(milk.IsCompatibleWith(new DietaryFlags(true, false, false, false)));
        Assert.False(milk.IsCompatibleWith(new DietaryFlags(false, true, false, false)));   // not gluten free
    }

    [Fact]
    public void Food_without_meal_list_suits_every_meal()
    {
        Assert.True(new Food().IsSuitableForMeal(MealTypes.Dinner));
        Assert.True(new Food { SuitableMeals = "Breakfast, Snack" }.IsSuitableForMeal("snack"));
        Assert.False(new Food { SuitableMeals = "Breakfast, Snack" }.IsSuitableForMeal(MealTypes.Lunch));
    }

    [Fact]
    public void Allergens_are_normalised_to_canonical_casing()
    {
        Assert.Equal("Soy,Egg", FoodAllergens.Normalize("soy, EGG ,egg"));
        Assert.Null(FoodAllergens.Normalize("  "));
    }

    [Fact]
    public void Unknown_allergens_are_reported()
    {
        Assert.Empty(FoodAllergens.Unknown("Egg, Soy"));
        Assert.Equal(new[] { "pollen" }, FoodAllergens.Unknown("Egg, pollen").ToArray());
    }
}
