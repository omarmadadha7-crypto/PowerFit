using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DataAccess.Entities;

public partial class ExercisePlan
{
    /// <summary>Requires ExercisePlanItems to be loaded.</summary>
    [NotMapped] public int TotalExercises => ExercisePlanItems.Count;

    /// <summary>Number of distinct training days that actually have exercises.</summary>
    [NotMapped] public int ScheduledDays => ExercisePlanItems.Select(i => i.DayNumber).Distinct().Count();

    public bool IsScheduleValid() => ScheduledDays <= DaysPerWeek;

    public IEnumerable<IGrouping<int, ExercisePlanItem>> GetItemsByDay() => ExercisePlanItems
        .OrderBy(i => i.DayNumber)
        .ThenBy(i => i.OrderIndex)
        .GroupBy(i => i.DayNumber);

    public int GetNextOrderIndex(int dayNumber) => ExercisePlanItems
        .Where(i => i.DayNumber == dayNumber)
        .Select(i => i.OrderIndex)
        .DefaultIfEmpty(0)
        .Max() + 1;
}
