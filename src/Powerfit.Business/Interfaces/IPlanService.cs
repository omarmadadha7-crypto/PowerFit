using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Plans;

namespace Powerfit.Business.Interfaces;

/// <summary>
/// Plan generation and reading. <c>requesterUserId</c>: pass the caller's UserId for normal users
/// (the plan must belong to them, otherwise NotFound); pass null for admins.
/// </summary>
public interface IPlanService
{
    /// <summary>Builds a plan with the AI from the user's profile. Throws AiGenerationException on AI failure.</summary>
    Task<FitnessPlanDto> GenerateAsync(int userId, GeneratePlanRequest request, CancellationToken ct = default);

    Task<FitnessPlanDto> GetAsync(int planId, int? requesterUserId = null, CancellationToken ct = default);

    /// <summary>The user's active plan, or null when there is none.</summary>
    Task<FitnessPlanDto?> GetActiveAsync(int userId, CancellationToken ct = default);

    /// <summary>User's plans (newest first). Empty Draft plans left by failed generations are always hidden.</summary>
    Task<IReadOnlyList<FitnessPlanSummaryDto>> GetUserPlansAsync(int userId, string? status = null, CancellationToken ct = default);

    /// <summary>Admin listing.</summary>
    Task<PagedResponse<FitnessPlanSummaryDto>> SearchAsync(PlanQuery query, CancellationToken ct = default);

    /// <summary>Makes the plan Active and archives the user's other active plans.</summary>
    Task<FitnessPlanDto> ActivateAsync(int planId, int? requesterUserId = null, CancellationToken ct = default);

    Task<FitnessPlanDto> ArchiveAsync(int planId, int? requesterUserId = null, CancellationToken ct = default);

    /// <summary>Active plans must be archived first.</summary>
    Task DeleteAsync(int planId, int? requesterUserId = null, CancellationToken ct = default);
}
