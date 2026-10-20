using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Repositories;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Exercises;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Mapping;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Services;

public class ExerciseService(IUnitOfWork uow) : IExerciseService
{
    public async Task<PagedResponse<ExerciseDto>> SearchAsync(ExerciseQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var criteria = new ExerciseSearch(
            query.Term, query.MuscleGroup, query.ExerciseType,
            query.DifficultyLevel, query.Equipment, query.IsActive);

        var page = await uow.Exercises.SearchAsync(criteria, query.Page, query.PageSize, ct);
        return PagedResponse<ExerciseDto>.From(page, e => e.ToDto());
    }

    public async Task<ExerciseDto> GetAsync(int exerciseId, CancellationToken ct = default)
    {
        var exercise = await uow.Exercises.GetByIdAsync(exerciseId, ct)
            ?? throw new NotFoundException("Exercise", exerciseId);

        return exercise.ToDto();
    }

    public async Task<ExerciseDto> CreateAsync(CreateExerciseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ExerciseValidator.Validate(
            request.Name, request.MuscleGroup, request.ExerciseType,
            request.Equipment, request.DifficultyLevel, request.ContraindicatedAreas);

        if (await uow.Exercises.NameExistsAsync(request.Name, null, ct))
            throw new ConflictException($"An exercise named '{request.Name.Trim()}' already exists.");

        var exercise = request.ToEntity();
        await uow.Exercises.AddAsync(exercise, ct);
        await uow.SaveChangesAsync(ct);

        return exercise.ToDto();
    }

    public async Task<ExerciseDto> UpdateAsync(int exerciseId, UpdateExerciseRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ExerciseValidator.Validate(
            request.Name, request.MuscleGroup, request.ExerciseType,
            request.Equipment, request.DifficultyLevel, request.ContraindicatedAreas);

        var exercise = await uow.Exercises.GetByIdAsync(exerciseId, ct)
            ?? throw new NotFoundException("Exercise", exerciseId);

        if (await uow.Exercises.NameExistsAsync(request.Name, exerciseId, ct))
            throw new ConflictException($"An exercise named '{request.Name.Trim()}' already exists.");

        exercise.Apply(request);
        await uow.SaveChangesAsync(ct);

        return exercise.ToDto();
    }

    public async Task<ExerciseDto> SetActiveAsync(int exerciseId, bool isActive, CancellationToken ct = default)
    {
        if (!await uow.Exercises.SetActiveAsync(exerciseId, isActive, ct))
            throw new NotFoundException("Exercise", exerciseId);

        await uow.SaveChangesAsync(ct);
        return await GetAsync(exerciseId, ct);
    }

    public async Task DeleteAsync(int exerciseId, CancellationToken ct = default)
    {
        var exercise = await uow.Exercises.GetByIdAsync(exerciseId, ct)
            ?? throw new NotFoundException("Exercise", exerciseId);

        if (await uow.Exercises.IsUsedInPlansAsync(exerciseId, ct))
            throw new ConflictException("This exercise is used in existing plans. Deactivate it instead of deleting it.");

        uow.Exercises.Remove(exercise);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetMuscleGroupsAsync(CancellationToken ct = default) =>
        await uow.Exercises.GetMuscleGroupsAsync(ct);

    public async Task<IReadOnlyList<string>> GetEquipmentAsync(CancellationToken ct = default) =>
        await uow.Exercises.GetEquipmentAsync(ct);
}
