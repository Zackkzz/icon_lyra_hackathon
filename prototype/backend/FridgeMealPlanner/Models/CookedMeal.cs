using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FridgeMealPlanner.Models;

// A batch of a recipe the user has actually cooked. One cook can yield multiple
// portions; each portion can be planned onto a day in the calendar.
public class CookedMeal
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? RecipeId { get; set; }

    // Snapshot of the recipe name at cook time (survives recipe deletion).
    [Required]
    [MaxLength(300)]
    public string RecipeName { get; set; } = string.Empty;

    public int Portions { get; set; }

    // Ingredient cost of this prep, computed at cook time from ingredient prices.
    [Column(TypeName = "decimal(10,2)")]
    public decimal Cost { get; set; }

    public DateTime CookedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RecipeId))]
    public Recipe? Recipe { get; set; }

    public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
}
