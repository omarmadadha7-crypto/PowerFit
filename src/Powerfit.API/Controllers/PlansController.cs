using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Powerfit.API.Extensions;
using Powerfit.Business.DTOs.Plans;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers;

/// <summary>The signed-in user's own plans.</summary>
[ApiController]
[Authorize(Policy = AuthPolicies.UserOnly)]
[Route("api/plans")]
public class PlansController(IPlanService plans) : ControllerBase
{
    /// <summary>
    /// Generates a new plan with the AI from the user's profile (the profile must be complete).
    /// The body is optional: send {} to use the defaults.
    /// </summary>
    [HttpPost("generate")]
    [EnableRateLimiting(RateLimitPolicies.AiGeneration)]
    [RequestTimeout("AiGeneration")]
    public async Task<ActionResult<FitnessPlanDto>> Generate(GeneratePlanRequest? request, CancellationToken ct)
    {
        var plan = await plans.GenerateAsync(User.GetUserId(), request ?? new GeneratePlanRequest(), ct);
        return CreatedAtAction(nameof(GetById), new { id = plan.PlanId }, plan);
    }

    /// <summary>The user's plans (newest first). Optional filter: ?status=Active|Archived|Draft.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FitnessPlanSummaryDto>>> List([FromQuery] string? status, CancellationToken ct) =>
        Ok(await plans.GetUserPlansAsync(User.GetUserId(), status, ct));

    /// <summary>The user's active plan (404 when there is none).</summary>
    [HttpGet("active")]
    public async Task<ActionResult<FitnessPlanDto>> GetActive(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var plan = await plans.GetActiveAsync(userId, ct)
            ?? throw new NotFoundException("The user has no active plan.");

        return Ok(plan);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FitnessPlanDto>> GetById(int id, CancellationToken ct) =>
        Ok(await plans.GetAsync(id, User.GetUserId(), ct));

    /// <summary>Makes the plan active and archives the previous active one.</summary>
    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<FitnessPlanDto>> Activate(int id, CancellationToken ct) =>
        Ok(await plans.ActivateAsync(id, User.GetUserId(), ct));

    [HttpPost("{id:int}/archive")]
    public async Task<ActionResult<FitnessPlanDto>> Archive(int id, CancellationToken ct) =>
        Ok(await plans.ArchiveAsync(id, User.GetUserId(), ct));

    /// <summary>Deletes a plan. An active plan must be archived first (409 otherwise).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await plans.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }
}
