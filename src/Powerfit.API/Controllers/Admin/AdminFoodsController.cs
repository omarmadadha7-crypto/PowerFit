using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.API.Extensions;
using Powerfit.API.Requests;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Foods;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers.Admin;

/// <summary>Food catalog management (includes inactive foods).</summary>
[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("api/admin/foods")]
public class AdminFoodsController(IFoodService foods) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<FoodDto>>> Search([FromQuery] FoodQuery query, CancellationToken ct) =>
        Ok(await foods.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FoodDto>> GetById(int id, CancellationToken ct) =>
        Ok(await foods.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<FoodDto>> Create(CreateFoodRequest request, CancellationToken ct)
    {
        var food = await foods.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = food.FoodId }, food);
    }

    /// <summary>Full replace of the food.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FoodDto>> Update(int id, UpdateFoodRequest request, CancellationToken ct) =>
        Ok(await foods.UpdateAsync(id, request, ct));

    /// <summary>Enable / disable a food (inactive ones are not used for new plans).</summary>
    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult<FoodDto>> SetActive(int id, SetActiveRequest request, CancellationToken ct) =>
        Ok(await foods.SetActiveAsync(id, request.IsActive, ct));

    /// <summary>Deletes a food. 409 when it is used in a plan: deactivate it instead.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await foods.DeleteAsync(id, ct);
        return NoContent();
    }
}
