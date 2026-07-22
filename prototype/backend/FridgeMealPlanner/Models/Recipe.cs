using System.ComponentModel.DataAnnotations;

namespace FridgeMealPlanner.Models;

public class Recipe
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string Instructions { get; set; } = string.Empty;

    public int Servings { get; set; }

    public int PrepTimeMinutes { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
}
