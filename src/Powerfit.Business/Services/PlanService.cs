using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Powerfit.Business.AI;
using Powerfit.Business.DTOs.Common;
using Powerfit.Business.DTOs.Plans;
using Powerfit.Business.Exceptions;
using Powerfit.Business.Interfaces;
using Powerfit.Business.Mapping;
using Powerfit.Business.Settings;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Services;

public class PlanService(
    IUnitOfWork uow,
    IAiPlanGenerator ai,
    IOptions<AiSettings> aiOptions,
    ILogger<PlanService> logger) : IPlanService
{
    private readonly AiSettings _limits = aiOptions.Value;

    // ========================================================================
    //  Generate
    // ========================================================================

    public async Task<FitnessPlanDto> GenerateAsync(int userId, GeneratePlanRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PlanValidator.Validate(request);

        var profile = await uow.Profiles.GetWithUserAsync(userId, ct);
        if (profile is null || !profile.IsComplete)
            throw new ValidationException("profile", "Complete your profile before generating a plan.");

        // Birth date, minimum age, Fat Loss for minors / underweight users (no silent default age any more).
        var age = SafetyRules.EnsureEligibleForPlan(profile, profile.User.Person);

        // Cheap checks first: no catalog queries, no draft row and no AI call when a quota is used up.
        await EnforceGenerationLimitsAsync(userId, ct);

        var daysPerWeek = request.DaysPerWeek ?? profile.PreferredDaysPerWeek!.Value;
        var language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim().ToLowerInvariant();

        // What the AI is allowed to use: matches level, equipment, injuries and dietary restrictions.
        var exercises = await uow.Exercises.GetCandidatesAsync(ExerciseCandidateFilter.ForProfile(profile), ct);
        if (exercises.Count == 0)
            throw new ValidationException("exercises", "No suitable exercises were found for this profile (level, equipment or injuries).");

        var foods = await uow.Foods.GetCandidatesAsync(
            new FoodSearch(IsActive: true, Dietary: profile.GetDietaryFlags()), ct);
        if (foods.Count == 0)
            throw new ValidationException("foods", "No suitable foods were found for this profile's dietary restrictions.");

        var aiRequest = BuildAiRequest(profile, age, daysPerWeek, language, request.AdditionalNotes, exercises, foods);

        // 1) A Draft plan first: AIGenerationLog.PlanID is a required foreign key.
        var plan = new FitnessPlan { UserId = userId };
        await uow.FitnessPlans.AddAsync(plan, ct);
        await uow.SaveChangesAsync(ct);

        try
        {
            // 2) The AI call. No DB transaction is open while we wait for the HTTP answer.
            var outcome = await ai.GenerateAsync(aiRequest, ct);

            await uow.AiGenerationLogs.AddLogAsync(
                plan.PlanId, outcome.Model, outcome.RequestJson, outcome.ResponseJson, outcome.Status, ct);

            if (!outcome.Succeeded)
            {
                // Keep the log (and the empty Draft plan it points to) so failures can be reviewed.
                await uow.SaveChangesAsync(ct);
                throw new AiGenerationException(
                    outcome.Error ?? "The AI could not generate a plan.",
                    outcome.Status == GenerationStatuses.Timeout);
            }

            var draft = outcome.Plan!;

            // 3) Save log + exercise plan + nutrition plan + activation as one unit.
            await uow.ExecuteInTransactionAsync(async token =>
            {
                await uow.ExercisePlans.AddAsync(BuildExercisePlan(plan.PlanId, daysPerWeek, draft.ExercisePlan!), token);
                await uow.NutritionPlans.AddAsync(BuildNutritionPlan(plan.PlanId, draft.NutritionPlan!), token);

                if (request.ActivateImmediately)
                    await uow.FitnessPlans.ActivateAsync(plan.PlanId, token);

                await uow.SaveChangesAsync(token);
            }, ct);
        }
        catch (Exception ex) when (ex is not AiGenerationException)
        {
            // Unexpected problem (config, DB, cancellation...): do not leave an orphan Draft plan behind.
            await TryDeleteDraftAsync(plan.PlanId);
            throw;
        }

        return await GetAsync(plan.PlanId, userId, ct);
    }

    // ========================================================================
    //  Read
    // ========================================================================

    public async Task<FitnessPlanDto> GetAsync(int planId, int? requesterUserId = null, CancellationToken ct = default)
    {
        var plan = await uow.FitnessPlans.GetWithDetailsAsync(planId, asNoTracking: true, ct: ct)
            ?? throw new NotFoundException("Fitness plan", planId);

        if (requesterUserId is { } uid && plan.UserId != uid)
            throw new NotFoundException("Fitness plan", planId); // do not reveal that it exists

        return plan.ToDto();
    }

    public async Task<FitnessPlanDto?> GetActiveAsync(int userId, CancellationToken ct = default)
    {
        var plan = await uow.FitnessPlans.GetActiveByUserAsync(
            userId, withDetails: true, asNoTracking: true, ct: ct);

        return plan?.ToDto();
    }

    public async Task<IReadOnlyList<FitnessPlanSummaryDto>> GetUserPlansAsync(
        int userId, string? status = null, CancellationToken ct = default)
    {
        var canonicalStatus = MappingHelpers.CanonicalOrNull(status, AllowedValues.PlanStatusValues);
        if (canonicalStatus is not null)
            PlanValidator.ValidateStatus(canonicalStatus);

        // Only the EMPTY drafts (failed generations) are hidden: a generated plan the user did not activate
        // (ActivateImmediately = false) is a Draft with content and must stay in the list.
        var plans = await uow.FitnessPlans.GetByUserAsync(userId, canonicalStatus, excludeEmptyDrafts: true, ct: ct);

        return plans.Select(p => p.ToSummaryDto()).ToList();
    }

    public async Task<PagedResponse<FitnessPlanSummaryDto>> SearchAsync(PlanQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var status = MappingHelpers.CanonicalOrNull(query.Status, AllowedValues.PlanStatusValues);
        if (status is not null)
            PlanValidator.ValidateStatus(status);

        var page = await uow.FitnessPlans.SearchAsync(query.UserId, status, query.Page, query.PageSize, ct);
        return PagedResponse<FitnessPlanSummaryDto>.From(page, p => p.ToSummaryDto());
    }

    // ========================================================================
    //  Status changes
    // ========================================================================

    public async Task<FitnessPlanDto> ActivateAsync(int planId, int? requesterUserId = null, CancellationToken ct = default)
    {
        await EnsureAccessAsync(planId, requesterUserId, ct);

        if (!await uow.ExercisePlans.ExistsForPlanAsync(planId, ct) ||
            !await uow.NutritionPlans.ExistsForPlanAsync(planId, ct))
        {
            throw new ConflictException("The plan has no exercise/nutrition content and cannot be activated.");
        }

        await uow.ExecuteInTransactionAsync(async token =>
        {
            if (!await uow.FitnessPlans.ActivateAsync(planId, token))
                throw new NotFoundException("Fitness plan", planId);

            await uow.SaveChangesAsync(token);
        }, ct);

        return await GetAsync(planId, null, ct);
    }

    public async Task<FitnessPlanDto> ArchiveAsync(int planId, int? requesterUserId = null, CancellationToken ct = default)
    {
        await EnsureAccessAsync(planId, requesterUserId, ct);

        if (!await uow.FitnessPlans.ArchiveAsync(planId, ct))
            throw new NotFoundException("Fitness plan", planId);

        await uow.SaveChangesAsync(ct);
        return await GetAsync(planId, null, ct);
    }

    public async Task DeleteAsync(int planId, int? requesterUserId = null, CancellationToken ct = default)
    {
        var plan = await EnsureAccessAsync(planId, requesterUserId, ct);

        if (plan.IsCurrentlyActive)
            throw new ConflictException("Archive the active plan before deleting it.");

        // Cascades to exercise plan, nutrition plan, their items and the AI logs.
        uow.FitnessPlans.Remove(plan);
        await uow.SaveChangesAsync(ct);
    }

    // ========================================================================
    //  Helpers
    // ========================================================================

    /// <summary>Loads the plan (tracked). Throws NotFound when it does not exist or belongs to someone else.</summary>
    private async Task<FitnessPlan> EnsureAccessAsync(int planId, int? requesterUserId, CancellationToken ct)
    {
        var plan = await uow.FitnessPlans.GetByIdAsync(planId, ct)
            ?? throw new NotFoundException("Fitness plan", planId);

        if (requesterUserId is { } uid && plan.UserId != uid)
            throw new NotFoundException("Fitness plan", planId);

        return plan;
    }

    private async Task TryDeleteDraftAsync(int planId)
    {
        try
        {
            uow.ClearChangeTracker();

            var draft = await uow.FitnessPlans.GetByIdAsync(planId, CancellationToken.None);
            if (draft is { IsDraft: true })
            {
                uow.FitnessPlans.Remove(draft);
                await uow.SaveChangesAsync(CancellationToken.None);
            }
        }
        catch (Exception cleanupError)
        {
            logger.LogWarning(cleanupError, "Could not delete draft plan {PlanId} after a failed generation.", planId);
        }
    }

    /// <summary>
    /// Rolling 24 h quotas (the rate limiter only protects per hour and per process).
    /// Failed attempts count too, because the provider charges for them.
    /// </summary>
    private async Task EnforceGenerationLimitsAsync(int userId, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-24);

        if (_limits.MaxGenerationsPerUserPerDay > 0 &&
            await uow.AiGenerationLogs.CountSinceAsync(since, userId, ct) >= _limits.MaxGenerationsPerUserPerDay)
        {
            throw new TooManyRequestsException(
                $"You reached the limit of {_limits.MaxGenerationsPerUserPerDay} plan generations per day. Try again tomorrow.",
                TimeSpan.FromHours(1));
        }

        if (_limits.MaxGenerationsPerDay > 0 &&
            await uow.AiGenerationLogs.CountSinceAsync(since, null, ct) >= _limits.MaxGenerationsPerDay)
        {
            throw new TooManyRequestsException(
                "Plan generation is at capacity today. Please try again later.", TimeSpan.FromHours(1));
        }
    }

    private static AiPlanRequest BuildAiRequest(
        Profile profile,
        int age,
        int daysPerWeek,
        string language,
        string? additionalNotes,
        IReadOnlyList<Exercise> exercises,
        IReadOnlyList<Food> foods)
    {
        // The profile is complete here (IsComplete), so the nullable columns have values.
        var snapshot = new AiProfileSnapshot(
            age,
            profile.Gender!,
            profile.HeightCm!.Value,
            profile.WeightKg!.Value,
            profile.Bmi,
            profile.Goal!,
            profile.ActivityLevel!,
            profile.ExperienceLevel!,
            profile.TrainingLocation!,
            profile.InjuryList,
            profile.DietaryRestrictionList,
            profile.CalculateTdee(age));

        return new AiPlanRequest(
            snapshot,
            daysPerWeek,
            language,
            MappingHelpers.Clean(additionalNotes),
            exercises
                .Select(e => new AiExerciseOption(
                    e.ExerciseId, e.Name, e.MuscleGroup, e.ExerciseType, e.Equipment, e.DifficultyLevel))
                .ToList(),
            foods
                .Select(f => new AiFoodOption(
                    f.FoodId, f.Name, f.Category, f.SuitableMeals,
                    f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g))
                .ToList());
    }

    /// <summary>OrderIndex is renumbered 1..n per day (the AI's own numbers are only used for ordering).</summary>
    private static ExercisePlan BuildExercisePlan(int planId, int daysPerWeek, AiExercisePlanDraft draft)
    {
        var plan = new ExercisePlan
        {
            PlanId = planId,
            DaysPerWeek = daysPerWeek,
            Notes = draft.Notes
        };

        foreach (var day in draft.Items.GroupBy(i => i.DayNumber).OrderBy(g => g.Key))
        {
            var order = 1;
            foreach (var item in day.OrderBy(i => i.OrderIndex))
            {
                plan.ExercisePlanItems.Add(new ExercisePlanItem
                {
                    ExerciseId = item.ExerciseId,
                    DayNumber = day.Key,
                    OrderIndex = order++,
                    Sets = item.Sets,
                    Reps = item.Reps,
                    Source = ItemSources.AI,
                    Notes = item.Notes
                });
            }
        }

        return plan;
    }

    private static NutritionPlan BuildNutritionPlan(int planId, AiNutritionPlanDraft draft)
    {
        var plan = new NutritionPlan
        {
            PlanId = planId,
            DailyCalories = (int)Math.Round(draft.DailyCalories),
            ProteinG = (int)Math.Round(draft.ProteinG),
            CarbsG = (int)Math.Round(draft.CarbsG),
            FatG = (int)Math.Round(draft.FatG)
        };

        foreach (var item in draft.Items.OrderBy(i => i.DayNumber).ThenBy(i => MealTypes.Order(i.MealType)))
        {
            plan.NutritionPlanItems.Add(new NutritionPlanItem
            {
                FoodId = item.FoodId,
                DayNumber = item.DayNumber,
                MealType = item.MealType,
                QuantityGrams = Math.Clamp((int)Math.Round(item.QuantityGrams), 1, 2000),
                Source = ItemSources.AI
            });
        }

        return plan;
    }
}
