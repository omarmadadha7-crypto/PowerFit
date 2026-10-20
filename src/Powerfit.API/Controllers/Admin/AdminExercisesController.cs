using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.API.Extensions;
using Powerfit.API.Requests;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Exercises;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers.Admin;

/// <summary>Exercise catalog management (includes inactive exercises).</summary>
[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("api/admin/exercises")]
public class AdminExercisesController(IExerciseService exercises) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ExerciseDto>>> Search([FromQuery] ExerciseQuery query, CancellationToken ct) =>
        Ok(await exercises.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> GetById(int id, CancellationToken ct) =>
        Ok(await exercises.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ExerciseDto>> Create(CreateExerciseRequest request, CancellationToken ct)
    {
        var exercise = await exercises.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = exercise.ExerciseId }, exercise);
    }

    /// <summary>Full replace of the exercise.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> Update(int id, UpdateExerciseRequest request, CancellationToken ct) =>
        Ok(await exercises.UpdateAsync(id, request, ct));

    /// <summary>Enable / disable an exercise (inactive ones are not used for new plans).</summary>
    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult<ExerciseDto>> SetActive(int id, SetActiveRequest request, CancellationToken ct) =>
        Ok(await exercises.SetActiveAsync(id, request.IsActive, ct));

    /// <summary>Deletes an exercise. 409 when it is used in a plan: deactivate it instead.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await exercises.DeleteAsync(id, ct);
        return NoContent();
    }
}
