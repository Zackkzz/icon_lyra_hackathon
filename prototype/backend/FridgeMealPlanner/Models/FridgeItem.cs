using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.Models;

public class FridgeItem
{
    [Key]
    public int Id { get; set; }

    public int IngredientId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    public Unit Unit { get; set; }

    public DateTime BestBeforeDate { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Source Source { get; set; } = Source.Manual;

    [ForeignKey(nameof(IngredientId))]
    public Ingredient Ingredient { get; set; } = null!;
}
