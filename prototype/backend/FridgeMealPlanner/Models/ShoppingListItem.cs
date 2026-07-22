using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.Models;

public class ShoppingListItem
{
    [Key]
    public int Id { get; set; }

    public int ShoppingListId { get; set; }

    [Required]
    [MaxLength(200)]
    public string IngredientName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Quantity { get; set; }

    public Unit Unit { get; set; }

    public bool Purchased { get; set; }

    [ForeignKey(nameof(ShoppingListId))]
    public ShoppingList ShoppingList { get; set; } = null!;
}
