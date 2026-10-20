using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Entities;
using DataAccess.Repositories;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Foods;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Mapping;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Services;

public class FoodService(IUnitOfWork uow) : IFoodService
{
    public async Task<PagedResponse<FoodDto>> SearchAsync(FoodQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var criteria = new FoodSearch(
            Term: query.Term,
            Category: query.Category,
            MealType: MappingHelpers.CanonicalOrNull(query.MealType, AllowedValues.MealTypeValues),
            IsActive: query.IsActive,
            Dietary: new DietaryFlags(
                query.VegetarianOnly, query.GlutenFreeOnly, query.ExcludeDairy, query.ExcludeNuts));

        var page = await uow.Foods.SearchAsync(criteria, query.Page, query.PageSize, ct);
        return PagedResponse<FoodDto>.From(page, f => f.ToDto());
    }

    public async Task<FoodDto> GetAsync(int foodId, CancellationToken ct = default)
    {
        var food = await uow.Foods.GetByIdAsync(foodId, ct)
            ?? throw new NotFoundException("Food", foodId);

        return food.ToDto();
    }

    public async Task<FoodDto> CreateAsync(CreateFoodRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        FoodValidator.Validate(
            request.Name, request.Category, request.SuitableMeals,
            request.CaloriesPer100g, request.ProteinPer100g, request.CarbsPer100g, request.FatPer100g,
            request.Allergens);

        if (await uow.Foods.NameExistsAsync(request.Name, null, ct))
            throw new ConflictException($"A food named '{request.Name.Trim()}' already exists.");

        var food = request.ToEntity();
        await uow.Foods.AddAsync(food, ct);
        await uow.SaveChangesAsync(ct);

        return food.ToDto();
    }

    public async Task<FoodDto> UpdateAsync(int foodId, UpdateFoodRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        FoodValidator.Validate(
            request.Name, request.Category, request.SuitableMeals,
            request.CaloriesPer100g, request.ProteinPer100g, request.CarbsPer100g, request.FatPer100g,
            request.Allergens);

        var food = await uow.Foods.GetByIdAsync(foodId, ct)
            ?? throw new NotFoundException("Food", foodId);

        if (await uow.Foods.NameExistsAsync(request.Name, foodId, ct))
            throw new ConflictException($"A food named '{request.Name.Trim()}' already exists.");

        food.Apply(request);
        await uow.SaveChangesAsync(ct);

        return food.ToDto();
    }

    public async Task<FoodDto> SetActiveAsync(int foodId, bool isActive, CancellationToken ct = default)
    {
        if (!await uow.Foods.SetActiveAsync(foodId, isActive, ct))
            throw new NotFoundException("Food", foodId);

        await uow.SaveChangesAsync(ct);
        return await GetAsync(foodId, ct);
    }

    public async Task DeleteAsync(int foodId, CancellationToken ct = default)
    {
        var food = await uow.Foods.GetByIdAsync(foodId, ct)
            ?? throw new NotFoundException("Food", foodId);

        if (await uow.Foods.IsUsedInPlansAsync(foodId, ct))
            throw new ConflictException("This food is used in existing plans. Deactivate it instead of deleting it.");

        uow.Foods.Remove(food);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        await uow.Foods.GetCategoriesAsync(ct);
}
