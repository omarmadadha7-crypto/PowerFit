using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Exercises;

namespace Powerfit.Business.Interfaces;

public interface IExerciseService
{
    Task<PagedResponse<ExerciseDto>> SearchAsync(ExerciseQuery query, CancellationToken ct = default);
    Task<ExerciseDto> GetAsync(int exerciseId, CancellationToken ct = default);
    Task<ExerciseDto> CreateAsync(CreateExerciseRequest request, CancellationToken ct = default);
    Task<ExerciseDto> UpdateAsync(int exerciseId, UpdateExerciseRequest request, CancellationToken ct = default);
    Task<ExerciseDto> SetActiveAsync(int exerciseId, bool isActive, CancellationToken ct = default);

    /// <summary>Throws ConflictException when the exercise is used in a plan (deactivate it instead).</summary>
    Task DeleteAsync(int exerciseId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetMuscleGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEquipmentAsync(CancellationToken ct = default);
}
