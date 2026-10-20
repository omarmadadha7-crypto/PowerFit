using DataAccess.Entities;
using Xunit;

namespace Powerfit.Tests.Entities;

public class BodyAreasTests
{
    [Theory]
    [InlineData("knee pain", BodyAreas.Knee)]
    [InlineData("الركبة", BodyAreas.Knee)]
    [InlineData("رباط صليبي", BodyAreas.Knee)]
    [InlineData("left shoulder", BodyAreas.Shoulder)]
    [InlineData("ألم في الكتف", BodyAreas.Shoulder)]
    [InlineData("ankle sprain", BodyAreas.Ankle)]
    [InlineData("الكاحل", BodyAreas.Ankle)]
    [InlineData("المعصم", BodyAreas.Wrist)]
    [InlineData("الورك", BodyAreas.Hip)]
    [InlineData("tennis elbow", BodyAreas.Elbow)]
    [InlineData("الرقبة", BodyAreas.Neck)]
    [InlineData("lower back", BodyAreas.LowerBack)]
    [InlineData("أسفل الظهر", BodyAreas.LowerBack)]
    [InlineData("عرق النسا", BodyAreas.LowerBack)]
    public void Resolve_finds_the_area_in_english_and_arabic(string text, string expectedArea)
    {
        Assert.Contains(expectedArea, BodyAreas.Resolve(text));
    }

    [Fact]
    public void Plain_back_means_both_back_areas()
    {
        var areas = BodyAreas.Resolve("back pain");

        Assert.Contains(BodyAreas.LowerBack, areas);
        Assert.Contains(BodyAreas.UpperBack, areas);
    }

    [Fact]
    public void Lower_back_is_not_widened_to_upper_back()
    {
        var areas = BodyAreas.Resolve("lower back pain");

        Assert.Contains(BodyAreas.LowerBack, areas);
        Assert.DoesNotContain(BodyAreas.UpperBack, areas);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("headache")]
    [InlineData("ship")]        // "hip" must only match as a whole word
    public void Unknown_text_resolves_to_nothing(string text)
    {
        Assert.Empty(BodyAreas.Resolve(text));
    }

    [Fact]
    public void ResolveAll_unions_the_areas()
    {
        var areas = BodyAreas.ResolveAll(["knee", "الكتف"]);

        Assert.Contains(BodyAreas.Knee, areas);
        Assert.Contains(BodyAreas.Shoulder, areas);
    }
}
