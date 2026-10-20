using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.DTOs.Plans;

namespace Powerfit.Business.Interfaces;

/// <summary>
/// Admin edits of a generated plan. Every edit sets Source = Admin on the touched items and
/// stamps the plan (LastEditedByAdminID / LastEditedAt / UpdatedAt). Archived plans cannot be edited.
/// </summary>
public interface IPlanEditingService
{
    // ---------------- exercises ----------------
    Task<ExercisePlanDto> UpdateExercisePlanAsync(int adminId, int planId, UpdateExercisePlanRequest request, CancellationToken ct = default);
    Task<ExercisePlanItemDto> AddExerciseItemAsync(int adminId, int planId, AddExercisePlanItemRequest request, CancellationToken ct = default);
    Task<ExercisePlanItemDto> UpdateExerciseItemAsync(int adminId, int itemId, UpdateExercisePlanItemRequest request, CancellationToken ct = default);
    Task RemoveExerciseItemAsync(int adminId, int itemId, CancellationToken ct = default);

    // ---------------- nutrition ----------------
    Task<NutritionPlanDto> UpdateNutritionTargetsAsync(int adminId, int planId, UpdateNutritionTargetsRequest request, CancellationToken ct = default);
    Task<NutritionPlanItemDto> AddNutritionItemAsync(int adminId, int planId, AddNutritionPlanItemRequest request, CancellationToken ct = default);
    Task<NutritionPlanItemDto> UpdateNutritionItemAsync(int adminId, int itemId, UpdateNutritionPlanItemRequest request, CancellationToken ct = default);
    Task RemoveNutritionItemAsync(int adminId, int itemId, CancellationToken ct = default);
}
