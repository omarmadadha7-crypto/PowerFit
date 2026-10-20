using DataAccess.Entities;
using Xunit;

namespace Powerfit.Tests.Entities;

public class DietaryFlagsTests
{
    [Theory]
    [InlineData("vegetarian")]
    [InlineData("I am vegan")]
    [InlineData("نباتي")]
    public void Vegetarian_is_detected(string text)
    {
        Assert.True(DietaryFlags.FromText(text).Vegetarian);
    }

    [Theory]
    [InlineData("non-vegetarian")]
    [InlineData("not vegetarian")]
    [InlineData("غير نباتي")]
    public void Negated_vegetarian_is_not_vegetarian(string text)
    {
        Assert.False(DietaryFlags.FromText(text).Vegetarian);
    }

    [Fact]
    public void Gluten_dairy_and_nuts_are_detected()
    {
        var flags = DietaryFlags.FromText("celiac, lactose intolerant, peanut allergy");

        Assert.True(flags.GlutenFree);
        Assert.True(flags.NoDairy);
        Assert.True(flags.NoNuts);
        Assert.False(flags.Vegetarian);
    }

    [Fact]
    public void Arabic_restrictions_are_detected()
    {
        var flags = DietaryFlags.FromText("حساسية من المكسرات والحليب");

        Assert.True(flags.NoNuts);
        Assert.True(flags.NoDairy);
    }

    [Fact]
    public void The_word_nutritionist_is_not_a_nut_allergy()
    {
        Assert.False(DietaryFlags.FromText("my nutritionist said less salt").NoNuts);
    }

    [Theory]
    [InlineData("egg allergy", FoodAllergens.Egg)]
    [InlineData("حساسية من البيض", FoodAllergens.Egg)]
    [InlineData("allergic to soy", FoodAllergens.Soy)]
    [InlineData("fish", FoodAllergens.Fish)]
    [InlineData("shrimp allergy", FoodAllergens.Shellfish)]
    [InlineData("sesame", FoodAllergens.Sesame)]
    public void Allergens_are_detected(string text, string allergen)
    {
        Assert.Contains(allergen, DietaryFlags.FromText(text).ExcludedAllergens!);
    }

    [Fact]
    public void Eggplant_is_not_an_egg_allergy()
    {
        Assert.Null(DietaryFlags.FromText("I do not like eggplant").ExcludedAllergens);
    }

    [Fact]
    public void Shellfish_does_not_count_as_fish()
    {
        var allergens = DietaryFlags.FromText("shellfish allergy").ExcludedAllergens!;

        Assert.Contains(FoodAllergens.Shellfish, allergens);
        Assert.DoesNotContain(FoodAllergens.Fish, allergens);
    }

    [Fact]
    public void Empty_text_has_no_flags()
    {
        Assert.False(DietaryFlags.FromText(null).HasAny);
        Assert.False(DietaryFlags.FromText("  ").HasAny);
        Assert.False(default(DietaryFlags).HasAny);
    }
}
