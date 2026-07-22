using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShoppingListController : ControllerBase
{
    private readonly AppDbContext _db;

    public ShoppingListController(AppDbContext db) => _db = db;

    [HttpPost("generate")]
    public async Task<ActionResult<ShoppingListDto>> Generate([FromQuery] string week, [FromQuery] string userId = "default")
    {
        if (!DateOnly.TryParse(week, out var weekStart))
            return BadRequest("Invalid week date. Use format YYYY-MM-DD");

        var weekEnd = weekStart.AddDays(6);

        // Get all meal plans for this week
        var mealPlans = await _db.MealPlans
            .Include(mp => mp.Recipe)
            .ThenInclude(r => r!.RecipeIngredients)
            .Where(mp => mp.UserId == userId && mp.Date >= weekStart && mp.Date <= weekEnd && mp.RecipeId != null)
            .ToListAsync();

        // Aggregate all required ingredients
        var requiredIngredients = new Dictionary<int, (string Name, decimal TotalQty, Enums.Unit Unit)>();

        foreach (var mp in mealPlans)
        {
            if (mp.Recipe == null) continue;
            foreach (var ri in mp.Recipe.RecipeIngredients)
            {
                if (requiredIngredients.ContainsKey(ri.IngredientId))
                {
                    var existing = requiredIngredients[ri.IngredientId];
                    requiredIngredients[ri.IngredientId] = (existing.Name, existing.TotalQty + ri.Quantity, ri.Unit);
                }
                else
                {
                    var name = (await _db.Ingredients.FindAsync(ri.IngredientId))?.Name ?? $"Ingredient {ri.IngredientId}";
                    requiredIngredients[ri.IngredientId] = (name, ri.Quantity, ri.Unit);
                }
            }
        }

        // Get fridge contents
        var fridgeItems = await _db.FridgeItems
            .Include(f => f.Ingredient)
            .ToListAsync();

        var fridgeStock = fridgeItems.ToDictionary(
            f => f.IngredientId,
            f => (f.Quantity, f.Unit)
        );

        // Calculate missing items
        var missingItems = new List<ShoppingListItem>();
        foreach (var (ingredientId, (name, required, unit)) in requiredIngredients)
        {
            decimal missing = required;
            if (fridgeStock.TryGetValue(ingredientId, out var stock))
            {
                // Simple: only deduct if same unit. For a real app we'd do conversions.
                if (stock.Unit == unit)
                    missing = required - stock.Quantity;
                else
                    missing = required; // Different units – assume we need full amount
            }

            if (missing > 0)
            {
                missingItems.Add(new ShoppingListItem
                {
                    IngredientName = name,
                    Quantity = missing,
                    Unit = unit,
                    Purchased = false
                });
            }
        }

        var shoppingList = new ShoppingList
        {
            UserId = userId,
            WeekStartDate = weekStart,
            GeneratedAt = DateTime.UtcNow,
            ShoppingListItems = missingItems
        };

        _db.ShoppingLists.Add(shoppingList);
        await _db.SaveChangesAsync();

        return Ok(new ShoppingListDto(
            shoppingList.Id,
            shoppingList.WeekStartDate,
            shoppingList.GeneratedAt,
            shoppingList.ShoppingListItems.Select(sli => new ShoppingListItemDto(
                sli.Id, sli.IngredientName, sli.Quantity, sli.Unit, sli.Purchased
            )).ToList()
        ));
    }
}
