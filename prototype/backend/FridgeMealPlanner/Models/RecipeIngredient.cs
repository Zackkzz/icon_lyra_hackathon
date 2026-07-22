using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.Models;

public class RecipeIngredient
{
    [Key]
    public int Id { get; set; }

    public int RecipeId { get; set; }
    public int IngredientId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    public Unit Unit { get; set; }

    [ForeignKey(nameof(RecipeId))]
    public Recipe Recipe { get; set; } = null!;

    [ForeignKey(nameof(IngredientId))]
    public Ingredient Ingredient { get; set; } = null!;
}
