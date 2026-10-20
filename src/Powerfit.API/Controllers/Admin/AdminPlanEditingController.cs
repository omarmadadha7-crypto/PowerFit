using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.API.Extensions;
using Powerfit.Business.DTOs.Plans;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers.Admin;

/// <summary>
/// Admin edits of a generated plan. Every edit marks the touched items as Source = Admin and stamps the plan
/// (LastEditedByAdminID / LastEditedAt). Archived plans cannot be edited (409).
/// </summary>
[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("api/admin")]
public class AdminPlanEditingController(IPlanEditingService editing) : ControllerBase
{
    // ---------------------------------------------------------------- exercises

    /// <summary>Changes daysPerWeek and/or the notes of the plan's exercise plan.</summary>
    [HttpPut("plans/{planId:int}/exercise-plan")]
    public async Task<ActionResult<ExercisePlanDto>> UpdateExercisePlan(
        int planId, UpdateExercisePlanRequest request, CancellationToken ct) =>
        Ok(await editing.UpdateExercisePlanAsync(User.GetAdminId(), planId, request, ct));

    /// <summary>Adds an exercise to a day (omit orderIndex to append at the end of the day).</summary>
    [HttpPost("plans/{planId:int}/exercise-items")]
    public async Task<ActionResult<ExercisePlanItemDto>> AddExerciseItem(
        int planId, AddExercisePlanItemRequest request, CancellationToken ct)
    {
        var item = await editing.AddExerciseItemAsync(User.GetAdminId(), planId, request, ct);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    /// <summary>Changes the exercise, sets, reps or notes of one item (null = unchanged).</summary>
    [HttpPut("exercise-items/{itemId:int}")]
    public async Task<ActionResult<ExercisePlanItemDto>> UpdateExerciseItem(
        int itemId, UpdateExercisePlanItemRequest request, CancellationToken ct) =>
        Ok(await editing.UpdateExerciseItemAsync(User.GetAdminId(), itemId, request, ct));

    [HttpDelete("exercise-items/{itemId:int}")]
    public async Task<IActionResult> RemoveExerciseItem(int itemId, CancellationToken ct)
    {
        await editing.RemoveExerciseItemAsync(User.GetAdminId(), itemId, ct);
        return NoContent();
    }

    // ---------------------------------------------------------------- nutrition

    /// <summary>Changes the daily calories / macro targets (null = unchanged).</summary>
    [HttpPut("plans/{planId:int}/nutrition-targets")]
    public async Task<ActionResult<NutritionPlanDto>> UpdateNutritionTargets(
        int planId, UpdateNutritionTargetsRequest request, CancellationToken ct) =>
        Ok(await editing.UpdateNutritionTargetsAsync(User.GetAdminId(), planId, request, ct));

    [HttpPost("plans/{planId:int}/nutrition-items")]
    public async Task<ActionResult<NutritionPlanItemDto>> AddNutritionItem(
        int planId, AddNutritionPlanItemRequest request, CancellationToken ct)
    {
        var item = await editing.AddNutritionItemAsync(User.GetAdminId(), planId, request, ct);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    /// <summary>Changes the food, day, meal or quantity of one item (null = unchanged).</summary>
    [HttpPut("nutrition-items/{itemId:int}")]
    public async Task<ActionResult<NutritionPlanItemDto>> UpdateNutritionItem(
        int itemId, UpdateNutritionPlanItemRequest request, CancellationToken ct) =>
        Ok(await editing.UpdateNutritionItemAsync(User.GetAdminId(), itemId, request, ct));

    [HttpDelete("nutrition-items/{itemId:int}")]
    public async Task<IActionResult> RemoveNutritionItem(int itemId, CancellationToken ct)
    {
        await editing.RemoveNutritionItemAsync(User.GetAdminId(), itemId, ct);
        return NoContent();
    }
}
