using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.Models;

public class Ingredient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,4)")]
    public decimal? DensityGPerMl { get; set; }

    // Latest known price: PricePerUnit dollars buys one PriceUnit (e.g. $12 per
    // Kilogram). Captured from receipts or set manually; null when unknown.
    [Column(TypeName = "decimal(12,4)")]
    public decimal? PricePerUnit { get; set; }

    public Unit? PriceUnit { get; set; }

    public ICollection<FridgeItem> FridgeItems { get; set; } = new List<FridgeItem>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}
