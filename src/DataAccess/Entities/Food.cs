using System;
using System.Collections.Generic;

namespace DataAccess.Entities;

public partial class Food
{
    public int FoodId { get; set; }

    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? SuitableMeals { get; set; }

    public decimal CaloriesPer100g { get; set; }

    public decimal ProteinPer100g { get; set; }

    public decimal CarbsPer100g { get; set; }

    public decimal FatPer100g { get; set; }

    public bool IsVegetarian { get; set; }

    public bool IsGlutenFree { get; set; }

    public bool ContainsDairy { get; set; }

    public bool ContainsNuts { get; set; }

    public bool IsActive { get; set; }

    public string? Allergens { get; set; }

    public virtual ICollection<NutritionPlanItem> NutritionPlanItems { get; set; } = new List<NutritionPlanItem>();
}
