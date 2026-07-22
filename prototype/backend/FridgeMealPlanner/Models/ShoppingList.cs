using System.ComponentModel.DataAnnotations;

namespace FridgeMealPlanner.Models;

public class ShoppingList
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string UserId { get; set; } = string.Empty;

    public DateOnly WeekStartDate { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ShoppingListItem> ShoppingListItems { get; set; } = new List<ShoppingListItem>();
}
