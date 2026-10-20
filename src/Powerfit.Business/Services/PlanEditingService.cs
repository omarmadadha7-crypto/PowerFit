using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Entities;
using DataAccess.Repositories;
using Powerfit.Business.DTOs.Plans;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Mapping;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Services;

public class PlanEditingService(IUnitOfWork uow) : IPlanEditingService
{
    // ========================================================================
    //  Exercises
    // ========================================================================

    public async Task<ExercisePlanDto> UpdateExercisePlanAsync(
        int adminId, int planId, UpdateExercisePlanRequest request, CancellationToken ct = default)
    {
        PlanValidator.Validate(request);

        var plan = await LoadPlanForEditAsync(adminId, planId, ct);
        var exercisePlan = await uow.ExercisePlans.GetByPlanIdAsync(planId, includeItems: true, ct: ct)
            ?? throw new NotFoundException("Exercise plan of plan", planId);

        if (request.DaysPerWeek is { } days)
        {
            var usedDays = exercisePlan.ScheduledDays;
            if (days < usedDays)
                throw new ValidationException("daysPerWeek",
                    $"The plan already has exercises on {usedDays} different days. Remove them first.");

            exercisePlan.DaysPerWeek = days;
        }

        if (request.Notes is not null)
            exercisePlan.Notes = MappingHelpers.Clean(request.Notes);

        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);

        return exercisePlan.ToDto();
    }

    public async Task<ExercisePlanItemDto> AddExerciseItemAsync(
        int adminId, int planId, AddExercisePlanItemRequest request, CancellationToken ct = default)
    {
        PlanValidator.Validate(request);

        var plan = await LoadPlanForEditAsync(adminId, planId, ct);
        var exercisePlan = await uow.ExercisePlans.GetByPlanIdAsync(planId, includeItems: false, ct: ct)
            ?? throw new NotFoundException("Exercise plan of plan", planId);
        var exercise = await LoadActiveExerciseAsync(request.ExerciseId, ct);
        await EnsureExerciseIsSafeAsync(plan, exercise, request.OverrideSafetyCheck, ct);

        var existing = await uow.ExercisePlanItems.FindAsync(i => i.ExercisePlanId == exercisePlan.ExercisePlanId, ct);

        // The plan is limited to DaysPerWeek different training days.
        var usedDays = existing.Select(i => i.DayNumber).ToHashSet();
        if (!usedDays.Contains(request.DayNumber) && usedDays.Count >= exercisePlan.DaysPerWeek)
            throw new ValidationException("dayNumber",
                $"The plan is limited to {exercisePlan.DaysPerWeek} training days. Increase daysPerWeek first.");

        var sameDay = existing.Where(i => i.DayNumber == request.DayNumber).ToList();
        int orderIndex;

        if (request.OrderIndex is { } requested)
        {
            if (sameDay.Any(i => i.OrderIndex == requested))
                throw new ConflictException(
                    $"Day {request.DayNumber} already has an exercise at position {requested}. Omit orderIndex to append.");

            orderIndex = requested;
        }
        else
        {
            orderIndex = sameDay.Select(i => i.OrderIndex).DefaultIfEmpty(0).Max() + 1;
        }

        var item = new ExercisePlanItem
        {
            ExercisePlanId = exercisePlan.ExercisePlanId,
            Exercise = exercise,
            DayNumber = request.DayNumber,
            OrderIndex = orderIndex,
            Sets = request.Sets,
            Reps = request.Reps,
            Notes = MappingHelpers.Clean(request.Notes),
            Source = ItemSources.Admin
        };

        await uow.ExercisePlanItems.AddAsync(item, ct);
        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);

        return item.ToDto();
    }

    public async Task<ExercisePlanItemDto> UpdateExerciseItemAsync(
        int adminId, int itemId, UpdateExercisePlanItemRequest request, CancellationToken ct = default)
    {
        PlanValidator.Validate(request);

        var item = await uow.ExercisePlanItems.GetWithExerciseAsync(itemId, ct)
            ?? throw new NotFoundException("Exercise plan item", itemId);
        var plan = await LoadPlanOfExerciseItemAsync(adminId, item, ct);

        if (request.ExerciseId is { } exerciseId && exerciseId != item.ExerciseId)
        {
            var newExercise = await LoadActiveExerciseAsync(exerciseId, ct);
            await EnsureExerciseIsSafeAsync(plan, newExercise, request.OverrideSafetyCheck, ct);
            item.Exercise = newExercise;
        }

        if (request.Sets is { } sets) item.Sets = sets;
        if (request.Reps is { } reps) item.Reps = reps;
        if (request.Notes is not null) item.Notes = MappingHelpers.Clean(request.Notes);

        item.Source = ItemSources.Admin;
        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);

        return item.ToDto();
    }

    public async Task RemoveExerciseItemAsync(int adminId, int itemId, CancellationToken ct = default)
    {
        var item = await uow.ExercisePlanItems.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Exercise plan item", itemId);
        var plan = await LoadPlanOfExerciseItemAsync(adminId, item, ct);

        uow.ExercisePlanItems.Remove(item);
        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);
    }

    // ========================================================================
    //  Nutrition
    // ========================================================================

    public async Task<NutritionPlanDto> UpdateNutritionTargetsAsync(
        int adminId, int planId, UpdateNutritionTargetsRequest request, CancellationToken ct = default)
    {
        PlanValidator.Validate(request);

        var plan = await LoadPlanForEditAsync(adminId, planId, ct);
        var nutritionPlan = await uow.NutritionPlans.GetByPlanIdAsync(planId, includeItems: true, ct: ct)
            ?? throw new NotFoundException("Nutrition plan of plan", planId);

        var calories = request.DailyCalories ?? nutritionPlan.DailyCalories;
        var protein = request.ProteinG ?? nutritionPlan.ProteinG;
        var carbs = request.CarbsG ?? nutritionPlan.CarbsG;
        var fat = request.FatG ?? nutritionPlan.FatG;

        // Check BEFORE changing the entity: protein/carbs 4 kcal per gram, fat 9 kcal per gram.
        var macroCalories = protein * 4 + carbs * 4 + fat * 9;
        if (Math.Abs(macroCalories - calories) > calories * PlanValidator.MacroTolerance)
            throw new ValidationException("macros",
                $"The macros give {macroCalories} kcal but dailyCalories is {calories} (max difference {PlanValidator.MacroTolerance:P0}).");

        nutritionPlan.DailyCalories = calories;
        nutritionPlan.ProteinG = protein;
        nutritionPlan.CarbsG = carbs;
        nutritionPlan.FatG = fat;

        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);

        return nutritionPlan.ToDto();
    }

    public async Task<NutritionPlanItemDto> AddNutritionItemAsync(
        int adminId, int planId, AddNutritionPlanItemRequest request, CancellationToken ct = default)
    {
        PlanValidator.Validate(request);

        var plan = await LoadPlanForEditAsync(adminId, planId, ct);
        var nutritionPlan = await uow.NutritionPlans.GetByPlanIdAsync(planId, includeItems: false, ct: ct)
            ?? throw new NotFoundException("Nutrition plan of plan", planId);
        var food = await LoadActiveFoodAsync(request.FoodId, ct);
        await EnsureFoodMatchesDietAsync(plan, food, request.OverrideSafetyCheck, ct);

        var item = new NutritionPlanItem
        {
            NutritionPlanId = nutritionPlan.NutritionPlanId,
            Food = food,
            DayNumber = request.DayNumber,
            MealType = MappingHelpers.Canonical(request.MealType, AllowedValues.MealTypeValues),
            QuantityGrams = request.QuantityGrams,
            Source = ItemSources.Admin
        };

        await uow.NutritionPlanItems.AddAsync(item, ct);
        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);

        return item.ToDto();
    }

    public async Task<NutritionPlanItemDto> UpdateNutritionItemAsync(
        int adminId, int itemId, UpdateNutritionPlanItemRequest request, CancellationToken ct = default)
    {
        PlanValidator.Validate(request);

        var item = await uow.NutritionPlanItems.GetWithFoodAsync(itemId, ct)
            ?? throw new NotFoundException("Nutrition plan item", itemId);
        var plan = await LoadPlanOfNutritionItemAsync(adminId, item, ct);

        if (request.FoodId is { } foodId && foodId != item.FoodId)
        {
            var newFood = await LoadActiveFoodAsync(foodId, ct);
            await EnsureFoodMatchesDietAsync(plan, newFood, request.OverrideSafetyCheck, ct);
            item.Food = newFood;
        }

        if (request.DayNumber is { } day) item.DayNumber = day;
        if (request.QuantityGrams is { } grams) item.QuantityGrams = grams;
        if (request.MealType is not null)
            item.MealType = MappingHelpers.Canonical(request.MealType, AllowedValues.MealTypeValues);

        item.Source = ItemSources.Admin;
        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);

        return item.ToDto();
    }

    public async Task RemoveNutritionItemAsync(int adminId, int itemId, CancellationToken ct = default)
    {
        var item = await uow.NutritionPlanItems.GetByIdAsync(itemId, ct)
            ?? throw new NotFoundException("Nutrition plan item", itemId);
        var plan = await LoadPlanOfNutritionItemAsync(adminId, item, ct);

        uow.NutritionPlanItems.Remove(item);
        plan.MarkEditedByAdmin(adminId);
        await uow.SaveChangesAsync(ct);
    }

    // ========================================================================
    //  Helpers
    // ========================================================================

    /// <summary>Checks the admin exists and loads the plan (tracked). Archived plans cannot be edited.</summary>
    private async Task<FitnessPlan> LoadPlanForEditAsync(int adminId, int planId, CancellationToken ct)
    {
        if (!await uow.Admins.AnyAsync(a => a.AdminId == adminId, ct))
            throw new UnauthorizedException("Admin account not found.");

        var plan = await uow.FitnessPlans.GetByIdAsync(planId, ct)
            ?? throw new NotFoundException("Fitness plan", planId);

        if (plan.IsArchived)
            throw new ConflictException("Archived plans cannot be edited.");

        return plan;
    }

    private async Task<FitnessPlan> LoadPlanOfExerciseItemAsync(int adminId, ExercisePlanItem item, CancellationToken ct)
    {
        var exercisePlan = await uow.ExercisePlans.GetByIdAsync(item.ExercisePlanId, ct)
            ?? throw new NotFoundException("Exercise plan", item.ExercisePlanId);

        return await LoadPlanForEditAsync(adminId, exercisePlan.PlanId, ct);
    }

    private async Task<FitnessPlan> LoadPlanOfNutritionItemAsync(int adminId, NutritionPlanItem item, CancellationToken ct)
    {
        var nutritionPlan = await uow.NutritionPlans.GetByIdAsync(item.NutritionPlanId, ct)
            ?? throw new NotFoundException("Nutrition plan", item.NutritionPlanId);

        return await LoadPlanForEditAsync(adminId, nutritionPlan.PlanId, ct);
    }

    /// <summary>
    /// Admin edits used to bypass the injury check the AI path has. Rejects an exercise that is contraindicated
    /// for the plan owner's injuries unless the admin explicitly overrides it.
    /// </summary>
    private async Task EnsureExerciseIsSafeAsync(FitnessPlan plan, Exercise exercise, bool overrideCheck, CancellationToken ct)
    {
        if (overrideCheck) return;

        var profile = await uow.Profiles.GetByUserIdAsync(plan.UserId, ct);
        if (profile is null || profile.InjuryList.Count == 0 || exercise.IsSafeFor(profile.InjuryList))
            return;

        throw new ValidationException("exerciseId",
            $"'{exercise.Name}' is contraindicated for this user's injuries ({profile.Injuries}). " +
            "Set overrideSafetyCheck = true to use it anyway.");
    }

    /// <summary>Same idea for food: vegetarian / gluten / dairy / nut / allergen restrictions of the plan owner.</summary>
    private async Task EnsureFoodMatchesDietAsync(FitnessPlan plan, Food food, bool overrideCheck, CancellationToken ct)
    {
        if (overrideCheck) return;

        var profile = await uow.Profiles.GetByUserIdAsync(plan.UserId, ct);
        if (profile is null) return;

        var flags = profile.GetDietaryFlags();
        if (!flags.HasAny || food.IsCompatibleWith(flags)) return;

        throw new ValidationException("foodId",
            $"'{food.Name}' does not match this user's dietary restrictions ({profile.DietaryRestrictions}). " +
            "Set overrideSafetyCheck = true to use it anyway.");
    }

    private async Task<Exercise> LoadActiveExerciseAsync(int exerciseId, CancellationToken ct)
    {
        var exercise = await uow.Exercises.GetByIdAsync(exerciseId, ct)
            ?? throw new NotFoundException("Exercise", exerciseId);

        if (!exercise.IsActive)
            throw new ValidationException("exerciseId", "This exercise is inactive.");

        return exercise;
    }

    private async Task<Food> LoadActiveFoodAsync(int foodId, CancellationToken ct)
    {
        var food = await uow.Foods.GetByIdAsync(foodId, ct)
            ?? throw new NotFoundException("Food", foodId);

        if (!food.IsActive)
            throw new ValidationException("foodId", "This food is inactive.");

        return food;
    }
}
