namespace Powerfit.Business.DTOs.Plans;

/// <summary>Admin filter for the plans list.</summary>
public record PlanQuery
{
    public int? UserId { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
