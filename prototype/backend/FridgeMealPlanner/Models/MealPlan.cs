using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.Models;

public class MealPlan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public MealType MealType { get; set; }

    public int? RecipeId { get; set; }

    [ForeignKey(nameof(RecipeId))]
    public Recipe? Recipe { get; set; }
}
