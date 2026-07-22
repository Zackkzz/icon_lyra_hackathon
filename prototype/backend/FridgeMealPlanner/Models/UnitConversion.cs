using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.Models;

public class UnitConversion
{
    [Key]
    public int Id { get; set; }

    public Unit FromUnit { get; set; }

    public Unit ToUnit { get; set; }

    public int? IngredientId { get; set; }

    [Column(TypeName = "decimal(10,6)")]
    public decimal Multiplier { get; set; }

    [ForeignKey(nameof(IngredientId))]
    public Ingredient? Ingredient { get; set; }
}
