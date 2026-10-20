using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Foods;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers;

/// <summary>Read-only food catalog for any signed-in account. Inactive foods are hidden.</summary>
[ApiController]
[Authorize]
[Route("api/foods")]
public class FoodsController(IFoodService foods) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<FoodDto>>> Search([FromQuery] FoodQuery query, CancellationToken ct) =>
        Ok(await foods.SearchAsync(query with { IsActive = true }, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FoodDto>> GetById(int id, CancellationToken ct)
    {
        var food = await foods.GetAsync(id, ct);
        if (!food.IsActive)
            throw new NotFoundException("Food", id);

        return Ok(food);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken ct) =>
        Ok(await foods.GetCategoriesAsync(ct));
}
