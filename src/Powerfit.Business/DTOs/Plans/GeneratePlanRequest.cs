namespace Powerfit.Business.DTOs.Plans;

public record GeneratePlanRequest
{
    /// <summary>Overrides Profile.PreferredDaysPerWeek (1-7). Null = use the profile value.</summary>
    public int? DaysPerWeek { get; init; }

    /// <summary>Free text for the AI, e.g. "I only have dumbbells".</summary>
    public string? AdditionalNotes { get; init; }

    /// <summary>Language of the notes written by the AI ("en", "ar"...).</summary>
    public string Language { get; init; } = "en";

    /// <summary>Make the new plan Active (and archive the previous active plan) right away.</summary>
    public bool ActivateImmediately { get; init; } = true;
}
