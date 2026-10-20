using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.API.Extensions;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Plans;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers.Admin;

/// <summary>Admin view over every user's plans (no ownership check).</summary>
[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("api/admin/plans")]
public class AdminPlansController(IPlanService plans) : ControllerBase
{
    /// <summary>Filters: ?userId=&amp;status=&amp;page=&amp;pageSize=</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponse<FitnessPlanSummaryDto>>> Search([FromQuery] PlanQuery query, CancellationToken ct) =>
        Ok(await plans.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FitnessPlanDto>> GetById(int id, CancellationToken ct) =>
        Ok(await plans.GetAsync(id, null, ct));

    [HttpPost("{id:int}/activate")]
    public async Task<ActionResult<FitnessPlanDto>> Activate(int id, CancellationToken ct) =>
        Ok(await plans.ActivateAsync(id, null, ct));

    [HttpPost("{id:int}/archive")]
    public async Task<ActionResult<FitnessPlanDto>> Archive(int id, CancellationToken ct) =>
        Ok(await plans.ArchiveAsync(id, null, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await plans.DeleteAsync(id, null, ct);
        return NoContent();
    }
}
