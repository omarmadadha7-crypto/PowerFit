using System.Collections.Generic;

namespace Powerfit.Business.DTOs.Plans;

public record ExercisePlanDto(
    int ExercisePlanId,
    int DaysPerWeek,
    string? Notes,
    IReadOnlyList<ExercisePlanDayDto> Days);

public record ExercisePlanDayDto(
    int DayNumber,
    IReadOnlyList<ExercisePlanItemDto> Items);
