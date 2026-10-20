using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Exercises;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;

namespace Powerfit.API.Controllers;

/// <summary>Read-only exercise catalog for any signed-in account. Inactive exercises are hidden.</summary>
[ApiController]
[Authorize]
[Route("api/exercises")]
public class ExercisesController(IExerciseService exercises) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ExerciseDto>>> Search([FromQuery] ExerciseQuery query, CancellationToken ct) =>
        Ok(await exercises.SearchAsync(query with { IsActive = true }, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExerciseDto>> GetById(int id, CancellationToken ct)
    {
        var exercise = await exercises.GetAsync(id, ct);
        if (!exercise.IsActive)
            throw new NotFoundException("Exercise", id);

        return Ok(exercise);
    }

    [HttpGet("muscle-groups")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetMuscleGroups(CancellationToken ct) =>
        Ok(await exercises.GetMuscleGroupsAsync(ct));

    [HttpGet("equipment")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetEquipment(CancellationToken ct) =>
        Ok(await exercises.GetEquipmentAsync(ct));
}
