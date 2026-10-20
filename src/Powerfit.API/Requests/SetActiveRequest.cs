namespace Powerfit.API.Requests;

/// <summary>Body of PATCH .../{id}/active (enable or disable an exercise / food).</summary>
public record SetActiveRequest
{
    /// <summary>Required: a missing value is rejected instead of silently becoming false.</summary>
    public required bool IsActive { get; init; }
}
