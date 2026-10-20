// ============================================================================
//  PowerFit | Repositories.cs
//  Generic repository + one repository per table/aggregate + Unit of Work.
//
//  Conventions
//   * Repositories NEVER call SaveChanges. Use IUnitOfWork.SaveChangesAsync().
//   * Single-entity getters (GetById / GetWith* / FirstOrDefault) are TRACKED
//     -> modify the entity, then call SaveChangesAsync().
//   * List / search / projection methods are AsNoTracking (read-only).
//   * RemoveAllForPlan / RemoveByDay / DeleteOlderThan use ExecuteDelete:
//     they run IMMEDIATELY and bypass the change tracker, so call them inside
//     IUnitOfWork.ExecuteInTransactionAsync(...).
//   * Registered by services.AddDataAccess() (which calls AddPowerFitRepositories()).
//   * FitnessPlans.ActivateAsync also runs an IMMEDIATE ExecuteUpdate: call it inside ExecuteInTransactionAsync.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Repositories;

// ============================================================================
// Common types
// ============================================================================

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public static class QueryableExtensions
{
    public const int MaxPageSize = 200;

    /// <summary>Counts, then fetches one page. The query MUST already be ordered.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(ct);
        var items = total == 0
            ? new List<T>()
            : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<T>(items, total, page, pageSize);
    }
}

internal static class Normalizer
{
    public static string Email(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}

/// <summary>Admin screen search for exercises.</summary>
public sealed record ExerciseSearch(
    string? Term = null,
    string? MuscleGroup = null,
    string? ExerciseType = null,
    string? DifficultyLevel = null,
    string? Equipment = null,
    bool? IsActive = null);

/// <summary>Filter used when building a plan (AI or manual): which exercises are allowed for this user.</summary>
public sealed record ExerciseCandidateFilter(
    string? MaxDifficulty = null,
    IReadOnlyCollection<string>? Equipment = null,
    IReadOnlyCollection<string>? InjuredAreas = null,
    IReadOnlyCollection<string>? MuscleGroups = null,
    IReadOnlyCollection<string>? ExerciseTypes = null)
{
    /// <summary>
    /// Gym => any equipment. Home/Outdoor => only <paramref name="homeEquipment"/> (default: bodyweight "None").
    /// "None" is always allowed.
    /// </summary>
    public static ExerciseCandidateFilter ForProfile(Profile profile, IReadOnlyCollection<string>? homeEquipment = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var anyEquipment = profile.TrainingLocation == TrainingLocations.Gym;
        IReadOnlyCollection<string>? equipment = anyEquipment
            ? null
            : (homeEquipment ?? new[] { Exercise.NoEquipment });

        return new ExerciseCandidateFilter(
            MaxDifficulty: profile.ExperienceLevel,
            Equipment: equipment,
            InjuredAreas: profile.InjuryList);
    }
}

public sealed record FoodSearch(
    string? Term = null,
    string? Category = null,
    string? MealType = null,
    bool? IsActive = null,
    DietaryFlags Dietary = default);

/// <summary>Light row for log lists (avoids loading the big JSON columns).</summary>
public sealed record AiLogSummary(
    int LogId,
    int PlanId,
    string Model,
    string Status,
    DateTime CreatedAt,
    int RequestLength,
    int? ResponseLength);

// ============================================================================
// Generic repository
// ============================================================================

public interface IGenericRepository<T> where T : class
{
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();

    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);

    Task<PagedResult<T>> GetPagedAsync(
        int page,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default);

    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected PowerFitDbContext Context { get; }
    protected DbSet<T> Set { get; }

    public GenericRepository(PowerFitDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Set = context.Set<T>();
    }

    public IQueryable<T> Query() => Set;
    public IQueryable<T> QueryNoTracking() => Set.AsNoTracking();

    // All entities in this model have a single int primary key.
    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await Set.FindAsync(new object[] { id }, ct);

    public virtual Task<List<T>> GetAllAsync(CancellationToken ct = default) =>
        QueryNoTracking().ToListAsync(ct);

    public virtual Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        QueryNoTracking().Where(predicate).ToListAsync(ct);

    public virtual Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(predicate, ct);

    public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);

    public virtual Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null ? Set.CountAsync(ct) : Set.CountAsync(predicate, ct);

    public virtual Task<PagedResult<T>> GetPagedAsync(
        int page,
        int pageSize,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(orderBy);

        IQueryable<T> query = QueryNoTracking();
        if (predicate is not null) query = query.Where(predicate);

        return orderBy(query).ToPagedResultAsync(page, pageSize, ct);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public virtual Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) =>
        Set.AddRangeAsync(entities, ct);

    /// <summary>Not needed for tracked entities. Use for detached entities (marks the whole graph).</summary>
    public virtual void Update(T entity) => Set.Update(entity);
    public virtual void UpdateRange(IEnumerable<T> entities) => Set.UpdateRange(entities);
    public virtual void Remove(T entity) => Set.Remove(entity);
    public virtual void RemoveRange(IEnumerable<T> entities) => Set.RemoveRange(entities);
}

// ============================================================================
// Person
// ============================================================================

public interface IPersonRepository : IGenericRepository<Person>
{
    Task<Person?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, int? excludePersonId = null, CancellationToken ct = default);

    /// <summary>Person + its User and Admin rows.</summary>
    Task<Person?> GetWithAccountsAsync(int personId, CancellationToken ct = default);

    Task<PagedResult<Person>> SearchAsync(
        string? term, string? role, int page, int pageSize, CancellationToken ct = default);
}

public class PersonRepository(PowerFitDbContext context)
    : GenericRepository<Person>(context), IPersonRepository
{
    public Task<Person?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var key = Normalizer.Email(email);
        return Set.FirstOrDefaultAsync(p => p.Email == key, ct);
    }

    public Task<bool> EmailExistsAsync(string email, int? excludePersonId = null, CancellationToken ct = default)
    {
        var key = Normalizer.Email(email);
        var query = QueryNoTracking().Where(p => p.Email == key);
        if (excludePersonId is { } id) query = query.Where(p => p.PersonId != id);
        return query.AnyAsync(ct);
    }

    public Task<Person?> GetWithAccountsAsync(int personId, CancellationToken ct = default) =>
        Set.Include(p => p.Users)
           .Include(p => p.Admins)
           .FirstOrDefaultAsync(p => p.PersonId == personId, ct);

    public Task<PagedResult<Person>> SearchAsync(
        string? term, string? role, int page, int pageSize, CancellationToken ct = default)
    {
        var query = QueryNoTracking();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim();
            query = query.Where(p => p.Role == r);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(p =>
                p.FirstName.Contains(t) ||
                p.LastName.Contains(t) ||
                p.Email.Contains(t) ||
                (p.FirstName + " " + p.LastName).Contains(t));
        }

        return query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ThenBy(p => p.PersonId)
            .ToPagedResultAsync(page, pageSize, ct);
    }
}

// ============================================================================
// User
// ============================================================================

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByPersonIdAsync(int personId, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>User + Person + Profile.</summary>
    Task<User?> GetWithPersonAndProfileAsync(int userId, CancellationToken ct = default);

    /// <summary>User + Person + Profile + all FitnessPlans (without plan details).</summary>
    Task<User?> GetWithPlansAsync(int userId, CancellationToken ct = default);

    Task<bool> ExistsForPersonAsync(int personId, CancellationToken ct = default);

    Task<PagedResult<User>> SearchAsync(
        string? term, bool? profileCompleted, int page, int pageSize, CancellationToken ct = default);
}

public class UserRepository(PowerFitDbContext context)
    : GenericRepository<User>(context), IUserRepository
{
    public Task<User?> GetByPersonIdAsync(int personId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(u => u.PersonId == personId, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var key = Normalizer.Email(email);
        return Set.Include(u => u.Person)
                  .Include(u => u.Profile)
                  .FirstOrDefaultAsync(u => u.Person.Email == key, ct);
    }

    public Task<User?> GetWithPersonAndProfileAsync(int userId, CancellationToken ct = default) =>
        Set.Include(u => u.Person)
           .Include(u => u.Profile)
           .FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<User?> GetWithPlansAsync(int userId, CancellationToken ct = default) =>
        Set.Include(u => u.Person)
           .Include(u => u.Profile)
           .Include(u => u.FitnessPlans)
           .AsSplitQuery()
           .FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public Task<bool> ExistsForPersonAsync(int personId, CancellationToken ct = default) =>
        QueryNoTracking().AnyAsync(u => u.PersonId == personId, ct);

    public Task<PagedResult<User>> SearchAsync(
        string? term, bool? profileCompleted, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<User> query = QueryNoTracking()
            .Include(u => u.Person)
            .Include(u => u.Profile);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(u =>
                u.Person.FirstName.Contains(t) ||
                u.Person.LastName.Contains(t) ||
                u.Person.Email.Contains(t) ||
                (u.Person.FirstName + " " + u.Person.LastName).Contains(t));
        }

        if (profileCompleted is { } done)
        {
            query = done
                ? query.Where(u => u.Profile != null && u.Profile.ProfileCompleted)
                : query.Where(u => u.Profile == null || !u.Profile.ProfileCompleted);
        }

        return query
            .OrderBy(u => u.Person.LastName).ThenBy(u => u.Person.FirstName).ThenBy(u => u.UserId)
            .ToPagedResultAsync(page, pageSize, ct);
    }
}

// ============================================================================
// Admin
// ============================================================================

public interface IAdminRepository : IGenericRepository<Admin>
{
    Task<Admin?> GetByPersonIdAsync(int personId, CancellationToken ct = default);
    Task<Admin?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Admin?> GetWithPersonAsync(int adminId, CancellationToken ct = default);
    Task<List<Admin>> GetAllWithPersonAsync(CancellationToken ct = default);
    Task<int> CountEditedPlansAsync(int adminId, CancellationToken ct = default);
}

public class AdminRepository(PowerFitDbContext context)
    : GenericRepository<Admin>(context), IAdminRepository
{
    public Task<Admin?> GetByPersonIdAsync(int personId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(a => a.PersonId == personId, ct);

    public Task<Admin?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var key = Normalizer.Email(email);
        return Set.Include(a => a.Person).FirstOrDefaultAsync(a => a.Person.Email == key, ct);
    }

    public Task<Admin?> GetWithPersonAsync(int adminId, CancellationToken ct = default) =>
        Set.Include(a => a.Person).FirstOrDefaultAsync(a => a.AdminId == adminId, ct);

    public Task<List<Admin>> GetAllWithPersonAsync(CancellationToken ct = default) =>
        QueryNoTracking()
            .Include(a => a.Person)
            .OrderBy(a => a.Person.LastName).ThenBy(a => a.Person.FirstName)
            .ToListAsync(ct);

    public Task<int> CountEditedPlansAsync(int adminId, CancellationToken ct = default) =>
        Context.FitnessPlans.CountAsync(p => p.LastEditedByAdminId == adminId, ct);
}

// ============================================================================
// Profile
// ============================================================================

public interface IProfileRepository : IGenericRepository<Profile>
{
    Task<Profile?> GetByUserIdAsync(int userId, CancellationToken ct = default);

    /// <summary>Profile + User + Person (Person gives BirthDate/Age for BMR calculations).</summary>
    Task<Profile?> GetWithUserAsync(int userId, CancellationToken ct = default);

    Task<bool> ExistsAsync(int userId, CancellationToken ct = default);
    Task<bool> IsCompletedAsync(int userId, CancellationToken ct = default);

    /// <summary>Creates the profile if missing, otherwise copies the fields onto the existing one.</summary>
    Task<Profile> UpsertAsync(int userId, Profile data, CancellationToken ct = default);
}

public class ProfileRepository(PowerFitDbContext context)
    : GenericRepository<Profile>(context), IProfileRepository
{
    public Task<Profile?> GetByUserIdAsync(int userId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<Profile?> GetWithUserAsync(int userId, CancellationToken ct = default) =>
        Set.Include(p => p.User).ThenInclude(u => u.Person)
           .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task<bool> ExistsAsync(int userId, CancellationToken ct = default) =>
        QueryNoTracking().AnyAsync(p => p.UserId == userId, ct);

    public Task<bool> IsCompletedAsync(int userId, CancellationToken ct = default) =>
        QueryNoTracking().AnyAsync(p => p.UserId == userId && p.ProfileCompleted, ct);

    public async Task<Profile> UpsertAsync(int userId, Profile data, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var existing = await Set.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (existing is null)
        {
            data.UserId = userId;
            data.RefreshCompletion();
            await Set.AddAsync(data, ct);
            return data;
        }

        existing.CopyFrom(data);
        return existing;
    }
}

// ============================================================================
// FitnessPlan
// ============================================================================

public interface IFitnessPlanRepository : IGenericRepository<FitnessPlan>
{
    /// <summary>Plan + ExercisePlan(items+exercise) + NutritionPlan(items+food). Split query.</summary>
    Task<FitnessPlan?> GetWithDetailsAsync(int planId, bool asNoTracking = false, CancellationToken ct = default);

    Task<FitnessPlan?> GetActiveByUserAsync(
        int userId, bool withDetails = true, bool asNoTracking = false, CancellationToken ct = default);

    Task<FitnessPlan?> GetLatestByUserAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// User's plans, newest first. <paramref name="excludeEmptyDrafts"/> hides the Draft plans that have no
    /// ExercisePlan (what a failed AI generation leaves behind); real Drafts (content, not activated) stay visible.
    /// </summary>
    Task<List<FitnessPlan>> GetByUserAsync(
        int userId, string? status = null, bool excludeEmptyDrafts = false, CancellationToken ct = default);

    /// <summary>Admin listing (includes User + Person). Any filter can be null.</summary>
    Task<PagedResult<FitnessPlan>> SearchAsync(
        int? userId, string? status, int page, int pageSize, CancellationToken ct = default);

    Task<List<FitnessPlan>> GetEditedByAdminAsync(int adminId, int take = 50, CancellationToken ct = default);

    Task<bool> HasActivePlanAsync(int userId, CancellationToken ct = default);
    Task<bool> BelongsToUserAsync(int planId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Makes the plan Active and archives the user's other Active plans.
    /// The other plans are archived IMMEDIATELY (ExecuteUpdate, bypasses the change tracker) so the filtered unique
    /// index UX_FitnessPlan_OneActivePerUser never sees two Active rows. The plan itself is only marked:
    /// call SaveChanges afterwards, and run both inside IUnitOfWork.ExecuteInTransactionAsync.
    /// Returns false if not found.
    /// </summary>
    Task<bool> ActivateAsync(int planId, CancellationToken ct = default);

    /// <summary>Archives the plan. Call SaveChanges afterwards. Returns false if not found.</summary>
    Task<bool> ArchiveAsync(int planId, CancellationToken ct = default);

    /// <summary>Number of plans per status (all three statuses are always present).</summary>
    Task<Dictionary<string, int>> CountByStatusAsync(CancellationToken ct = default);
}

public class FitnessPlanRepository(PowerFitDbContext context)
    : GenericRepository<FitnessPlan>(context), IFitnessPlanRepository
{
    private static IQueryable<FitnessPlan> IncludeDetails(IQueryable<FitnessPlan> query) =>
        query
            .Include(p => p.ExercisePlan).ThenInclude(e => e!.ExercisePlanItems).ThenInclude(i => i.Exercise)
            .Include(p => p.NutritionPlan).ThenInclude(n => n!.NutritionPlanItems).ThenInclude(i => i.Food)
            .AsSplitQuery();

    public Task<FitnessPlan?> GetWithDetailsAsync(
        int planId, bool asNoTracking = false, CancellationToken ct = default)
    {
        var query = IncludeDetails(asNoTracking ? QueryNoTracking() : Query());
        return query.FirstOrDefaultAsync(p => p.PlanId == planId, ct);
    }

    public Task<FitnessPlan?> GetActiveByUserAsync(
        int userId, bool withDetails = true, bool asNoTracking = false, CancellationToken ct = default)
    {
        IQueryable<FitnessPlan> query = asNoTracking ? QueryNoTracking() : Query();
        if (withDetails) query = IncludeDetails(query);

        return query
            .Where(p => p.UserId == userId && p.Status == PlanStatuses.Active)
            .OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.PlanId)
            .FirstOrDefaultAsync(ct);
    }

    public Task<FitnessPlan?> GetLatestByUserAsync(int userId, CancellationToken ct = default) =>
        QueryNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.PlanId)
            .FirstOrDefaultAsync(ct);

    public Task<List<FitnessPlan>> GetByUserAsync(
        int userId, string? status = null, bool excludeEmptyDrafts = false, CancellationToken ct = default)
    {
        var query = QueryNoTracking().Where(p => p.UserId == userId);

        if (excludeEmptyDrafts)
            query = query.Where(p => p.Status != PlanStatuses.Draft || p.ExercisePlan != null);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            query = query.Where(p => p.Status == s);
        }

        return query.OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.PlanId).ToListAsync(ct);
    }

    public Task<PagedResult<FitnessPlan>> SearchAsync(
        int? userId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<FitnessPlan> query = QueryNoTracking()
            .Include(p => p.User).ThenInclude(u => u.Person);

        if (userId is { } uid) query = query.Where(p => p.UserId == uid);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            query = query.Where(p => p.Status == s);
        }

        return query
            .OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.PlanId)
            .ToPagedResultAsync(page, pageSize, ct);
    }

    public Task<List<FitnessPlan>> GetEditedByAdminAsync(int adminId, int take = 50, CancellationToken ct = default) =>
        QueryNoTracking()
            .Where(p => p.LastEditedByAdminId == adminId)
            .OrderByDescending(p => p.LastEditedAt)
            .Take(Math.Clamp(take, 1, QueryableExtensions.MaxPageSize))
            .ToListAsync(ct);

    public Task<bool> HasActivePlanAsync(int userId, CancellationToken ct = default) =>
        QueryNoTracking().AnyAsync(p => p.UserId == userId && p.Status == PlanStatuses.Active, ct);

    public Task<bool> BelongsToUserAsync(int planId, int userId, CancellationToken ct = default) =>
        QueryNoTracking().AnyAsync(p => p.PlanId == planId && p.UserId == userId, ct);

    public async Task<bool> ActivateAsync(int planId, CancellationToken ct = default)
    {
        var plan = await Set.FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        if (plan is null) return false;

        // Archive first, directly in the database: EF would otherwise send the two UPDATEs in an order we do
        // not control, and the unique index (one Active plan per user) rejects a moment with two Active rows.
        var now = DateTime.UtcNow;
        await Set
            .Where(p => p.UserId == plan.UserId && p.PlanId != planId && p.Status == PlanStatuses.Active)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PlanStatuses.Archived)
                // never earlier than CreatedAt (CK_FitnessPlan_Dates)
                .SetProperty(p => p.UpdatedAt, p => p.CreatedAt > now ? p.CreatedAt : now), ct);

        plan.Activate();
        return true;
    }

    public async Task<bool> ArchiveAsync(int planId, CancellationToken ct = default)
    {
        var plan = await Set.FirstOrDefaultAsync(p => p.PlanId == planId, ct);
        if (plan is null) return false;

        plan.Archive();
        return true;
    }

    public async Task<Dictionary<string, int>> CountByStatusAsync(CancellationToken ct = default)
    {
        var counts = await QueryNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

        foreach (var status in PlanStatuses.All)
            counts.TryAdd(status, 0);

        return counts;
    }
}

// ============================================================================
// ExercisePlan
// ============================================================================

public interface IExercisePlanRepository : IGenericRepository<ExercisePlan>
{
    /// <summary>The ExercisePlan of a FitnessPlan (1:1). Optionally with items + exercises.</summary>
    Task<ExercisePlan?> GetByPlanIdAsync(int planId, bool includeItems = false, CancellationToken ct = default);

    Task<ExercisePlan?> GetWithItemsAsync(int exercisePlanId, CancellationToken ct = default);
    Task<bool> ExistsForPlanAsync(int planId, CancellationToken ct = default);
}

public class ExercisePlanRepository(PowerFitDbContext context)
    : GenericRepository<ExercisePlan>(context), IExercisePlanRepository
{
    public Task<ExercisePlan?> GetByPlanIdAsync(int planId, bool includeItems = false, CancellationToken ct = default)
    {
        IQueryable<ExercisePlan> query = Set;
        if (includeItems)
            query = query.Include(e => e.ExercisePlanItems).ThenInclude(i => i.Exercise);

        return query.FirstOrDefaultAsync(e => e.PlanId == planId, ct);
    }

    public Task<ExercisePlan?> GetWithItemsAsync(int exercisePlanId, CancellationToken ct = default) =>
        Set.Include(e => e.ExercisePlanItems).ThenInclude(i => i.Exercise)
           .FirstOrDefaultAsync(e => e.ExercisePlanId == exercisePlanId, ct);

    public Task<bool> ExistsForPlanAsync(int planId, CancellationToken ct = default) =>
        QueryNoTracking().AnyAsync(e => e.PlanId == planId, ct);
}

// ============================================================================
// ExercisePlanItem
// ============================================================================

public interface IExercisePlanItemRepository : IGenericRepository<ExercisePlanItem>
{
    Task<ExercisePlanItem?> GetWithExerciseAsync(int itemId, CancellationToken ct = default);

    /// <summary>All items of the plan with Exercise, ordered by day then order.</summary>
    Task<List<ExercisePlanItem>> GetByPlanAsync(int exercisePlanId, CancellationToken ct = default);

    Task<List<ExercisePlanItem>> GetByDayAsync(int exercisePlanId, int dayNumber, CancellationToken ct = default);

    /// <summary>MAX(OrderIndex)+1 for that day (1 when the day is empty).</summary>
    Task<int> GetNextOrderIndexAsync(int exercisePlanId, int dayNumber, CancellationToken ct = default);

    /// <summary>IMMEDIATE delete (bypasses the change tracker). Returns deleted rows.</summary>
    Task<int> RemoveAllForPlanAsync(int exercisePlanId, CancellationToken ct = default);

    /// <summary>IMMEDIATE delete (bypasses the change tracker). Returns deleted rows.</summary>
    Task<int> RemoveByDayAsync(int exercisePlanId, int dayNumber, CancellationToken ct = default);
}

public class ExercisePlanItemRepository(PowerFitDbContext context)
    : GenericRepository<ExercisePlanItem>(context), IExercisePlanItemRepository
{
    public Task<ExercisePlanItem?> GetWithExerciseAsync(int itemId, CancellationToken ct = default) =>
        Set.Include(i => i.Exercise).FirstOrDefaultAsync(i => i.ItemId == itemId, ct);

    public Task<List<ExercisePlanItem>> GetByPlanAsync(int exercisePlanId, CancellationToken ct = default) =>
        QueryNoTracking()
            .Include(i => i.Exercise)
            .Where(i => i.ExercisePlanId == exercisePlanId)
            .OrderBy(i => i.DayNumber).ThenBy(i => i.OrderIndex)
            .ToListAsync(ct);

    public Task<List<ExercisePlanItem>> GetByDayAsync(int exercisePlanId, int dayNumber, CancellationToken ct = default) =>
        QueryNoTracking()
            .Include(i => i.Exercise)
            .Where(i => i.ExercisePlanId == exercisePlanId && i.DayNumber == dayNumber)
            .OrderBy(i => i.OrderIndex)
            .ToListAsync(ct);

    public async Task<int> GetNextOrderIndexAsync(int exercisePlanId, int dayNumber, CancellationToken ct = default)
    {
        var max = await QueryNoTracking()
            .Where(i => i.ExercisePlanId == exercisePlanId && i.DayNumber == dayNumber)
            .MaxAsync(i => (int?)i.OrderIndex, ct);

        return (max ?? 0) + 1;
    }

    public Task<int> RemoveAllForPlanAsync(int exercisePlanId, CancellationToken ct = default) =>
        Set.Where(i => i.ExercisePlanId == exercisePlanId).ExecuteDeleteAsync(ct);

    public Task<int> RemoveByDayAsync(int exercisePlanId, int dayNumber, CancellationToken ct = default) =>
        Set.Where(i => i.ExercisePlanId == exercisePlanId && i.DayNumber == dayNumber).ExecuteDeleteAsync(ct);
}

// ============================================================================
// NutritionPlan
// ============================================================================

public interface INutritionPlanRepository : IGenericRepository<NutritionPlan>
{
    /// <summary>The NutritionPlan of a FitnessPlan (1:1). Optionally with items + foods.</summary>
    Task<NutritionPlan?> GetByPlanIdAsync(int planId, bool includeItems = false, CancellationToken ct = default);

    Task<NutritionPlan?> GetWithItemsAsync(int nutritionPlanId, CancellationToken ct = default);
    Task<bool> ExistsForPlanAsync(int planId, CancellationToken ct = default);
}

public class NutritionPlanRepository(PowerFitDbContext context)
    : GenericRepository<NutritionPlan>(context), INutritionPlanRepository
{
    public Task<NutritionPlan?> GetByPlanIdAsync(int planId, bool includeItems = false, CancellationToken ct = default)
    {
        IQueryable<NutritionPlan> query = Set;
        if (includeItems)
            query = query.Include(n => n.NutritionPlanItems).ThenInclude(i => i.Food);

        return query.FirstOrDefaultAsync(n => n.PlanId == planId, ct);
    }

    public Task<NutritionPlan?> GetWithItemsAsync(int nutritionPlanId, CancellationToken ct = default) =>
        Set.Include(n => n.NutritionPlanItems).ThenInclude(i => i.Food)
           .FirstOrDefaultAsync(n => n.NutritionPlanId == nutritionPlanId, ct);

    public Task<bool> ExistsForPlanAsync(int planId, CancellationToken ct = default) =>
        QueryNoTracking().AnyAsync(n => n.PlanId == planId, ct);
}

// ============================================================================
// NutritionPlanItem
// ============================================================================

public interface INutritionPlanItemRepository : IGenericRepository<NutritionPlanItem>
{
    Task<NutritionPlanItem?> GetWithFoodAsync(int itemId, CancellationToken ct = default);

    /// <summary>All items with Food, ordered by day, meal (Breakfast..Snack), then id.</summary>
    Task<List<NutritionPlanItem>> GetByPlanAsync(int nutritionPlanId, CancellationToken ct = default);

    Task<List<NutritionPlanItem>> GetByDayAsync(int nutritionPlanId, int dayNumber, CancellationToken ct = default);

    Task<List<NutritionPlanItem>> GetByMealAsync(
        int nutritionPlanId, int dayNumber, string mealType, CancellationToken ct = default);

    /// <summary>Calories/macros of one day (computed from the Food table).</summary>
    Task<NutritionTotals> GetDayTotalsAsync(int nutritionPlanId, int dayNumber, CancellationToken ct = default);

    /// <summary>Calories/macros per day (only days that have items).</summary>
    Task<Dictionary<int, NutritionTotals>> GetWeeklyTotalsAsync(int nutritionPlanId, CancellationToken ct = default);

    /// <summary>IMMEDIATE delete (bypasses the change tracker). Returns deleted rows.</summary>
    Task<int> RemoveAllForPlanAsync(int nutritionPlanId, CancellationToken ct = default);

    /// <summary>IMMEDIATE delete (bypasses the change tracker). Returns deleted rows.</summary>
    Task<int> RemoveByDayAsync(int nutritionPlanId, int dayNumber, CancellationToken ct = default);
}

public class NutritionPlanItemRepository(PowerFitDbContext context)
    : GenericRepository<NutritionPlanItem>(context), INutritionPlanItemRepository
{
    private IQueryable<NutritionPlanItem> OrderedWithFood(int nutritionPlanId) =>
        QueryNoTracking()
            .Include(i => i.Food)
            .Where(i => i.NutritionPlanId == nutritionPlanId)
            .OrderBy(i => i.DayNumber)
            .ThenBy(i => i.MealType == MealTypes.Breakfast ? 1
                       : i.MealType == MealTypes.Lunch ? 2
                       : i.MealType == MealTypes.Dinner ? 3 : 4)
            .ThenBy(i => i.ItemId);

    public Task<NutritionPlanItem?> GetWithFoodAsync(int itemId, CancellationToken ct = default) =>
        Set.Include(i => i.Food).FirstOrDefaultAsync(i => i.ItemId == itemId, ct);

    public Task<List<NutritionPlanItem>> GetByPlanAsync(int nutritionPlanId, CancellationToken ct = default) =>
        OrderedWithFood(nutritionPlanId).ToListAsync(ct);

    public Task<List<NutritionPlanItem>> GetByDayAsync(int nutritionPlanId, int dayNumber, CancellationToken ct = default) =>
        OrderedWithFood(nutritionPlanId).Where(i => i.DayNumber == dayNumber).ToListAsync(ct);

    public Task<List<NutritionPlanItem>> GetByMealAsync(
        int nutritionPlanId, int dayNumber, string mealType, CancellationToken ct = default) =>
        OrderedWithFood(nutritionPlanId)
            .Where(i => i.DayNumber == dayNumber && i.MealType == mealType)
            .ToListAsync(ct);

    public async Task<NutritionTotals> GetDayTotalsAsync(int nutritionPlanId, int dayNumber, CancellationToken ct = default)
    {
        var rows = await QueryNoTracking()
            .Where(i => i.NutritionPlanId == nutritionPlanId && i.DayNumber == dayNumber)
            .Select(i => new
            {
                i.QuantityGrams,
                i.Food.CaloriesPer100g,
                i.Food.ProteinPer100g,
                i.Food.CarbsPer100g,
                i.Food.FatPer100g
            })
            .ToListAsync(ct);

        return rows
            .Aggregate(NutritionTotals.Zero, (sum, r) => sum + NutritionTotals.From(
                r.QuantityGrams, r.CaloriesPer100g, r.ProteinPer100g, r.CarbsPer100g, r.FatPer100g))
            .Rounded();
    }

    public async Task<Dictionary<int, NutritionTotals>> GetWeeklyTotalsAsync(int nutritionPlanId, CancellationToken ct = default)
    {
        var rows = await QueryNoTracking()
            .Where(i => i.NutritionPlanId == nutritionPlanId)
            .Select(i => new
            {
                i.DayNumber,
                i.QuantityGrams,
                i.Food.CaloriesPer100g,
                i.Food.ProteinPer100g,
                i.Food.CarbsPer100g,
                i.Food.FatPer100g
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.DayNumber)
            .ToDictionary(
                g => g.Key,
                g => g.Aggregate(NutritionTotals.Zero, (sum, r) => sum + NutritionTotals.From(
                    r.QuantityGrams, r.CaloriesPer100g, r.ProteinPer100g, r.CarbsPer100g, r.FatPer100g)).Rounded());
    }

    public Task<int> RemoveAllForPlanAsync(int nutritionPlanId, CancellationToken ct = default) =>
        Set.Where(i => i.NutritionPlanId == nutritionPlanId).ExecuteDeleteAsync(ct);

    public Task<int> RemoveByDayAsync(int nutritionPlanId, int dayNumber, CancellationToken ct = default) =>
        Set.Where(i => i.NutritionPlanId == nutritionPlanId && i.DayNumber == dayNumber).ExecuteDeleteAsync(ct);
}

// ============================================================================
// Exercise
// ============================================================================

public interface IExerciseRepository : IGenericRepository<Exercise>
{
    Task<Exercise?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeExerciseId = null, CancellationToken ct = default);
    Task<List<Exercise>> GetActiveAsync(CancellationToken ct = default);

    Task<PagedResult<Exercise>> SearchAsync(
        ExerciseSearch criteria, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Active exercises allowed for a user: difficulty up to MaxDifficulty, matching equipment
    /// ("None" is always allowed), and excluding exercises contraindicated for the injured areas.
    /// </summary>
    Task<List<Exercise>> GetCandidatesAsync(ExerciseCandidateFilter filter, CancellationToken ct = default);

    Task<List<string>> GetMuscleGroupsAsync(CancellationToken ct = default);
    Task<List<string>> GetEquipmentAsync(CancellationToken ct = default);

    /// <summary>FK is NO ACTION: an exercise used in a plan cannot be deleted, deactivate it instead.</summary>
    Task<bool> IsUsedInPlansAsync(int exerciseId, CancellationToken ct = default);

    /// <summary>Soft enable/disable. Call SaveChanges afterwards. Returns false if not found.</summary>
    Task<bool> SetActiveAsync(int exerciseId, bool isActive, CancellationToken ct = default);
}

public class ExerciseRepository(PowerFitDbContext context)
    : GenericRepository<Exercise>(context), IExerciseRepository
{
    public Task<Exercise?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim();
        return Set.FirstOrDefaultAsync(e => e.Name == n, ct);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeExerciseId = null, CancellationToken ct = default)
    {
        var n = name.Trim();
        var query = QueryNoTracking().Where(e => e.Name == n);
        if (excludeExerciseId is { } id) query = query.Where(e => e.ExerciseId != id);
        return query.AnyAsync(ct);
    }

    public Task<List<Exercise>> GetActiveAsync(CancellationToken ct = default) =>
        QueryNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.MuscleGroup).ThenBy(e => e.Name)
            .ToListAsync(ct);

    public Task<PagedResult<Exercise>> SearchAsync(
        ExerciseSearch criteria, int page, int pageSize, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var query = QueryNoTracking();

        if (criteria.IsActive is { } active)
            query = query.Where(e => e.IsActive == active);

        if (!string.IsNullOrWhiteSpace(criteria.Term))
        {
            var t = criteria.Term.Trim();
            query = query.Where(e => e.Name.Contains(t) || e.MuscleGroup.Contains(t));
        }

        if (!string.IsNullOrWhiteSpace(criteria.MuscleGroup))
        {
            var m = criteria.MuscleGroup.Trim();
            query = query.Where(e => e.MuscleGroup == m);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ExerciseType))
        {
            var x = criteria.ExerciseType.Trim();
            query = query.Where(e => e.ExerciseType == x);
        }

        if (!string.IsNullOrWhiteSpace(criteria.DifficultyLevel))
        {
            var d = criteria.DifficultyLevel.Trim();
            query = query.Where(e => e.DifficultyLevel == d);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Equipment))
        {
            var q = criteria.Equipment.Trim();
            query = query.Where(e => e.Equipment == q);
        }

        return query
            .OrderBy(e => e.MuscleGroup).ThenBy(e => e.Name).ThenBy(e => e.ExerciseId)
            .ToPagedResultAsync(page, pageSize, ct);
    }

    public async Task<List<Exercise>> GetCandidatesAsync(ExerciseCandidateFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IQueryable<Exercise> query = QueryNoTracking().Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.MaxDifficulty))
        {
            var levels = DifficultyLevels.UpTo(filter.MaxDifficulty).ToList();
            query = query.Where(e => levels.Contains(e.DifficultyLevel));
        }

        if (filter.Equipment is { Count: > 0 })
        {
            var equipment = filter.Equipment
                .Append(Exercise.NoEquipment)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            query = query.Where(e => equipment.Contains(e.Equipment));
        }

        if (filter.MuscleGroups is { Count: > 0 })
        {
            var groups = filter.MuscleGroups.ToList();
            query = query.Where(e => groups.Contains(e.MuscleGroup));
        }

        if (filter.ExerciseTypes is { Count: > 0 })
        {
            var types = filter.ExerciseTypes.ToList();
            query = query.Where(e => types.Contains(e.ExerciseType));
        }

        var list = await query
            .OrderBy(e => e.MuscleGroup).ThenBy(e => e.Name)
            .ToListAsync(ct);

        // ContraindicatedAreas is free text, so this part is evaluated in memory.
        if (filter.InjuredAreas is { Count: > 0 })
            list = list.Where(e => e.IsSafeFor(filter.InjuredAreas)).ToList();

        return list;
    }

    public Task<List<string>> GetMuscleGroupsAsync(CancellationToken ct = default) =>
        QueryNoTracking().Select(e => e.MuscleGroup).Distinct().OrderBy(x => x).ToListAsync(ct);

    public Task<List<string>> GetEquipmentAsync(CancellationToken ct = default) =>
        QueryNoTracking().Select(e => e.Equipment).Distinct().OrderBy(x => x).ToListAsync(ct);

    public Task<bool> IsUsedInPlansAsync(int exerciseId, CancellationToken ct = default) =>
        Context.ExercisePlanItems.AsNoTracking().AnyAsync(i => i.ExerciseId == exerciseId, ct);

    public async Task<bool> SetActiveAsync(int exerciseId, bool isActive, CancellationToken ct = default)
    {
        var exercise = await Set.FirstOrDefaultAsync(e => e.ExerciseId == exerciseId, ct);
        if (exercise is null) return false;

        exercise.IsActive = isActive;
        return true;
    }
}

// ============================================================================
// Food
// ============================================================================

public interface IFoodRepository : IGenericRepository<Food>
{
    Task<Food?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeFoodId = null, CancellationToken ct = default);
    Task<List<Food>> GetActiveAsync(CancellationToken ct = default);

    Task<PagedResult<Food>> SearchAsync(FoodSearch criteria, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Non-paged version of SearchAsync, meant for building plans. Foods with no SuitableMeals
    /// are treated as suitable for every meal.
    /// </summary>
    Task<List<Food>> GetCandidatesAsync(FoodSearch criteria, CancellationToken ct = default);

    Task<List<string>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>FK is NO ACTION: a food used in a plan cannot be deleted, deactivate it instead.</summary>
    Task<bool> IsUsedInPlansAsync(int foodId, CancellationToken ct = default);

    /// <summary>Soft enable/disable. Call SaveChanges afterwards. Returns false if not found.</summary>
    Task<bool> SetActiveAsync(int foodId, bool isActive, CancellationToken ct = default);
}

public class FoodRepository(PowerFitDbContext context)
    : GenericRepository<Food>(context), IFoodRepository
{
    private static IQueryable<Food> ApplyCriteria(IQueryable<Food> query, FoodSearch criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        if (criteria.IsActive is { } active)
            query = query.Where(f => f.IsActive == active);

        if (!string.IsNullOrWhiteSpace(criteria.Term))
        {
            var t = criteria.Term.Trim();
            query = query.Where(f => f.Name.Contains(t) || f.Category.Contains(t));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            var c = criteria.Category.Trim();
            query = query.Where(f => f.Category == c);
        }

        if (!string.IsNullOrWhiteSpace(criteria.MealType))
        {
            var meal = criteria.MealType.Trim();
            query = query.Where(f => f.SuitableMeals == null || f.SuitableMeals.Contains(meal));
        }

        var diet = criteria.Dietary;
        if (diet.Vegetarian) query = query.Where(f => f.IsVegetarian);
        if (diet.GlutenFree) query = query.Where(f => f.IsGlutenFree);
        if (diet.NoDairy) query = query.Where(f => !f.ContainsDairy);
        if (diet.NoNuts) query = query.Where(f => !f.ContainsNuts);

        return query;
    }

    public Task<Food?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim();
        return Set.FirstOrDefaultAsync(f => f.Name == n, ct);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeFoodId = null, CancellationToken ct = default)
    {
        var n = name.Trim();
        var query = QueryNoTracking().Where(f => f.Name == n);
        if (excludeFoodId is { } id) query = query.Where(f => f.FoodId != id);
        return query.AnyAsync(ct);
    }

    public Task<List<Food>> GetActiveAsync(CancellationToken ct = default) =>
        QueryNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Category).ThenBy(f => f.Name)
            .ToListAsync(ct);

    public Task<PagedResult<Food>> SearchAsync(FoodSearch criteria, int page, int pageSize, CancellationToken ct = default) =>
        ApplyCriteria(QueryNoTracking(), criteria)
            .OrderBy(f => f.Category).ThenBy(f => f.Name).ThenBy(f => f.FoodId)
            .ToPagedResultAsync(page, pageSize, ct);

    public async Task<List<Food>> GetCandidatesAsync(FoodSearch criteria, CancellationToken ct = default)
    {
        var list = await ApplyCriteria(QueryNoTracking(), criteria)
            .OrderBy(f => f.Category).ThenBy(f => f.Name)
            .ToListAsync(ct);

        // Food.Allergens is a free-text list, so (like exercise injuries) it is applied in memory.
        // Only plan building needs it; the paged SearchAsync does not filter by allergen.
        if (criteria.Dietary.ExcludedAllergens is { Count: > 0 } allergens)
            list = list.Where(f => !f.ContainsAnyAllergen(allergens)).ToList();

        return list;
    }

    public Task<List<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        QueryNoTracking().Select(f => f.Category).Distinct().OrderBy(x => x).ToListAsync(ct);

    public Task<bool> IsUsedInPlansAsync(int foodId, CancellationToken ct = default) =>
        Context.NutritionPlanItems.AsNoTracking().AnyAsync(i => i.FoodId == foodId, ct);

    public async Task<bool> SetActiveAsync(int foodId, bool isActive, CancellationToken ct = default)
    {
        var food = await Set.FirstOrDefaultAsync(f => f.FoodId == foodId, ct);
        if (food is null) return false;

        food.IsActive = isActive;
        return true;
    }
}

// ============================================================================
// AIGenerationLog
// ============================================================================

public interface IAiGenerationLogRepository : IGenericRepository<AigenerationLog>
{
    /// <summary>All logs of a plan, newest first (includes the JSON payloads).</summary>
    Task<List<AigenerationLog>> GetByPlanAsync(int planId, CancellationToken ct = default);

    /// <summary>Newest log of a plan, optionally filtered by status (e.g. Success).</summary>
    Task<AigenerationLog?> GetLatestAsync(int planId, string? status = null, CancellationToken ct = default);

    /// <summary>Paged list WITHOUT the JSON payloads. Dates are UTC.</summary>
    Task<PagedResult<AiLogSummary>> SearchAsync(
        int? planId, string? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Logs per status (all three statuses are always present). Dates are UTC.</summary>
    Task<Dictionary<string, int>> CountByStatusAsync(
        DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);

    /// <summary>
    /// Number of generation attempts since <paramref name="sinceUtc"/> (all statuses: failed calls cost money too).
    /// <paramref name="userId"/> null = all users.
    /// </summary>
    Task<int> CountSinceAsync(DateTime sinceUtc, int? userId = null, CancellationToken ct = default);

    /// <summary>Validates (JSON, status rules) via AigenerationLog.Create and adds the row. Call SaveChanges afterwards.</summary>
    Task<AigenerationLog> AddLogAsync(
        int planId, string model, string requestJson, string? responseJson, string status,
        CancellationToken ct = default);

    /// <summary>IMMEDIATE delete (bypasses the change tracker). Returns deleted rows.</summary>
    Task<int> DeleteOlderThanAsync(DateTime beforeUtc, CancellationToken ct = default);
}

public class AiGenerationLogRepository(PowerFitDbContext context)
    : GenericRepository<AigenerationLog>(context), IAiGenerationLogRepository
{
    private static IQueryable<AigenerationLog> ApplyDates(
        IQueryable<AigenerationLog> query, DateTime? fromUtc, DateTime? toUtc)
    {
        if (fromUtc is { } from) query = query.Where(l => l.CreatedAt >= from);
        if (toUtc is { } to) query = query.Where(l => l.CreatedAt <= to);
        return query;
    }

    public Task<List<AigenerationLog>> GetByPlanAsync(int planId, CancellationToken ct = default) =>
        QueryNoTracking()
            .Where(l => l.PlanId == planId)
            .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.LogId)
            .ToListAsync(ct);

    public Task<AigenerationLog?> GetLatestAsync(int planId, string? status = null, CancellationToken ct = default)
    {
        var query = QueryNoTracking().Where(l => l.PlanId == planId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            query = query.Where(l => l.Status == s);
        }

        return query
            .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.LogId)
            .FirstOrDefaultAsync(ct);
    }

    public Task<PagedResult<AiLogSummary>> SearchAsync(
        int? planId, string? status, DateTime? fromUtc, DateTime? toUtc,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = ApplyDates(QueryNoTracking(), fromUtc, toUtc);

        if (planId is { } pid) query = query.Where(l => l.PlanId == pid);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            query = query.Where(l => l.Status == s);
        }

        return query
            .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.LogId)
            .Select(l => new AiLogSummary(
                l.LogId,
                l.PlanId,
                l.Model,
                l.Status,
                l.CreatedAt,
                l.RequestJson.Length,
                l.ResponseJson == null ? (int?)null : l.ResponseJson.Length))
            .ToPagedResultAsync(page, pageSize, ct);
    }

    public async Task<Dictionary<string, int>> CountByStatusAsync(
        DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        var counts = await ApplyDates(QueryNoTracking(), fromUtc, toUtc)
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

        counts.TryAdd(GenerationStatuses.Success, 0);
        counts.TryAdd(GenerationStatuses.Failed, 0);
        counts.TryAdd(GenerationStatuses.Timeout, 0);
        return counts;
    }

    public Task<int> CountSinceAsync(DateTime sinceUtc, int? userId = null, CancellationToken ct = default)
    {
        var query = QueryNoTracking().Where(l => l.CreatedAt >= sinceUtc);
        if (userId is { } uid) query = query.Where(l => l.Plan.UserId == uid);
        return query.CountAsync(ct);
    }

    public async Task<AigenerationLog> AddLogAsync(
        int planId, string model, string requestJson, string? responseJson, string status,
        CancellationToken ct = default)
    {
        var log = AigenerationLog.Create(planId, model, requestJson, responseJson, status);
        await Set.AddAsync(log, ct);
        return log;
    }

    public Task<int> DeleteOlderThanAsync(DateTime beforeUtc, CancellationToken ct = default) =>
        Set.Where(l => l.CreatedAt < beforeUtc).ExecuteDeleteAsync(ct);
}

// ============================================================================
// Unit of Work
// ============================================================================

public interface IUnitOfWork
{
    IPersonRepository People { get; }
    IUserRepository Users { get; }
    IAdminRepository Admins { get; }
    IProfileRepository Profiles { get; }
    IFitnessPlanRepository FitnessPlans { get; }
    IExercisePlanRepository ExercisePlans { get; }
    IExercisePlanItemRepository ExercisePlanItems { get; }
    INutritionPlanRepository NutritionPlans { get; }
    INutritionPlanItemRepository NutritionPlanItems { get; }
    IExerciseRepository Exercises { get; }
    IFoodRepository Foods { get; }
    IAiGenerationLogRepository AiGenerationLogs { get; }

    bool HasChanges { get; }
    Task<bool> CanConnectAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    void ClearChangeTracker();

    /// <summary>
    /// Runs <paramref name="action"/> inside one DB transaction (works with EnableRetryOnFailure).
    /// Call SaveChangesAsync inside the delegate; the transaction commits when it completes
    /// and rolls back if it throws. If a transaction is already open it is reused.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default);
}

/// <summary>
/// Register as Scoped. The DbContext is owned (and disposed) by the DI container, not by this class.
/// </summary>
public sealed class UnitOfWork(PowerFitDbContext context) : IUnitOfWork
{
    private IPersonRepository? _people;
    private IUserRepository? _users;
    private IAdminRepository? _admins;
    private IProfileRepository? _profiles;
    private IFitnessPlanRepository? _fitnessPlans;
    private IExercisePlanRepository? _exercisePlans;
    private IExercisePlanItemRepository? _exercisePlanItems;
    private INutritionPlanRepository? _nutritionPlans;
    private INutritionPlanItemRepository? _nutritionPlanItems;
    private IExerciseRepository? _exercises;
    private IFoodRepository? _foods;
    private IAiGenerationLogRepository? _aiGenerationLogs;

    public IPersonRepository People => _people ??= new PersonRepository(context);
    public IUserRepository Users => _users ??= new UserRepository(context);
    public IAdminRepository Admins => _admins ??= new AdminRepository(context);
    public IProfileRepository Profiles => _profiles ??= new ProfileRepository(context);
    public IFitnessPlanRepository FitnessPlans => _fitnessPlans ??= new FitnessPlanRepository(context);
    public IExercisePlanRepository ExercisePlans => _exercisePlans ??= new ExercisePlanRepository(context);
    public IExercisePlanItemRepository ExercisePlanItems => _exercisePlanItems ??= new ExercisePlanItemRepository(context);
    public INutritionPlanRepository NutritionPlans => _nutritionPlans ??= new NutritionPlanRepository(context);
    public INutritionPlanItemRepository NutritionPlanItems => _nutritionPlanItems ??= new NutritionPlanItemRepository(context);
    public IExerciseRepository Exercises => _exercises ??= new ExerciseRepository(context);
    public IFoodRepository Foods => _foods ??= new FoodRepository(context);
    public IAiGenerationLogRepository AiGenerationLogs => _aiGenerationLogs ??= new AiGenerationLogRepository(context);

    public bool HasChanges => context.ChangeTracker.HasChanges();

    public Task<bool> CanConnectAsync(CancellationToken ct = default) => context.Database.CanConnectAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);

    public void ClearChangeTracker() => context.ChangeTracker.Clear();

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteInTransactionAsync<bool>(async token =>
        {
            await action(token);
            return true;
        }, ct);
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Already inside a transaction: just join it.
            if (context.Database.CurrentTransaction is not null)
                return await action(ct);

            await using var tx = await context.Database.BeginTransactionAsync(ct);
            var result = await action(ct);
            await tx.CommitAsync(ct);
            return result;
        });
    }
}

// ============================================================================
// Dependency injection
// ============================================================================

public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers IUnitOfWork and every repository as Scoped.
    /// Requires AddDbContext&lt;PowerFitDbContext&gt;(...) to be registered first.
    /// </summary>
    public static IServiceCollection AddPowerFitRepositories(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IFitnessPlanRepository, FitnessPlanRepository>();
        services.AddScoped<IExercisePlanRepository, ExercisePlanRepository>();
        services.AddScoped<IExercisePlanItemRepository, ExercisePlanItemRepository>();
        services.AddScoped<INutritionPlanRepository, NutritionPlanRepository>();
        services.AddScoped<INutritionPlanItemRepository, NutritionPlanItemRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IFoodRepository, FoodRepository>();
        services.AddScoped<IAiGenerationLogRepository, AiGenerationLogRepository>();

        return services;
    }
}