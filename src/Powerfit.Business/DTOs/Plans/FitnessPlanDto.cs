using System;
using Powerfit.Business.Validators;

namespace Powerfit.Business.DTOs.Plans;

/// <summary>Full plan: exercise plan + nutrition plan.</summary>
public record FitnessPlanDto(
    int PlanId,
    int UserId,
    string Status,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastEditedAt,
    int? LastEditedByAdminId,
    ExercisePlanDto? ExercisePlan,
    NutritionPlanDto? NutritionPlan,
    string Disclaimer = SafetyRules.MedicalDisclaimer);

/// <summary>Row for plan lists (no details).</summary>
public record FitnessPlanSummaryDto(
    int PlanId,
    int UserId,
    string? UserFullName,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastEditedAt,
    int? LastEditedByAdminId);
