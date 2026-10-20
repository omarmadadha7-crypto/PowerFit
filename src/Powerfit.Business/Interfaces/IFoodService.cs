using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Foods;

namespace Powerfit.Business.Interfaces;

public interface IFoodService
{
    Task<PagedResponse<FoodDto>> SearchAsync(FoodQuery query, CancellationToken ct = default);
    Task<FoodDto> GetAsync(int foodId, CancellationToken ct = default);
    Task<FoodDto> CreateAsync(CreateFoodRequest request, CancellationToken ct = default);
    Task<FoodDto> UpdateAsync(int foodId, UpdateFoodRequest request, CancellationToken ct = default);
    Task<FoodDto> SetActiveAsync(int foodId, bool isActive, CancellationToken ct = default);

    /// <summary>Throws ConflictException when the food is used in a plan (deactivate it instead).</summary>
    Task DeleteAsync(int foodId, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);
}
