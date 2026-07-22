using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FridgeMealPlanner.Models;

// A recipe a user saved to use later.
public class RecipeBookmark
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RecipeId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RecipeId))]
    public Recipe? Recipe { get; set; }
}
