using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Enums;
using FridgeMealPlanner.Extensions;
using FridgeMealPlanner.Models;
using FridgeMealPlanner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CookedMealsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CookedMealsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<CookedMealDto>>> GetAll()
    {
        var userId = User.GetUserId();
        var meals = await _db.CookedMeals
            .Where(cm => cm.UserId == userId)
            .OrderByDescending(cm => cm.CookedAt)
            .Select(cm => new CookedMealDto(
                cm.Id,
                cm.RecipeId,
                cm.RecipeName,
                cm.Portions,
                cm.Portions - cm.MealPlans.Count(),
                cm.CookedAt))
            .ToListAsync();

        return Ok(meals);
    }

    // Cook a recipe: consumes the required ingredients from the fridge (scaled to the
    // number of portions) and records a batch. Blocks if the fridge is short.
    [HttpPost]
    public async Task<ActionResult<CookedMealDto>> Cook([FromBody] CookRecipeRequest request)
    {
        var userId = User.GetUserId();

        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && (r.UserId == null || r.UserId == userId));
        if (recipe == null)
            return NotFound(new { error = $"Recipe {request.RecipeId} not found" });

        var portions = Math.Clamp(request.Portions <= 0 ? recipe.Servings : request.Portions, 1, 50);
        // Recipe quantities are written for its base yield; scale to the portions cooked.
        var scale = portions / (double)Math.Max(1, recipe.Servings);

        var fridge = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .Include(f => f.Ingredient)
            .ToListAsync();

        // ---- sufficiency check ----
        var shortages = new List<string>();
        foreach (var ri in recipe.RecipeIngredients)
        {
            var requiredQty = ri.Quantity * (decimal)scale;
            var reqDim = UnitMath.DimensionOf(ri.Unit);
            var reqBase = UnitMath.ToBase(requiredQty, ri.Unit);

            var items = fridge.Where(f => f.IngredientId == ri.IngredientId).ToList();
            if (items.Count == 0)
            {
                shortages.Add($"{ri.Ingredient.Name} — need {Fmt(requiredQty)} {ri.Unit}, have none");
                continue;
            }

            // Only stock in the SAME dimension can be measured against the requirement.
            var sameDim = items.Where(f => UnitMath.DimensionOf(f.Unit) == reqDim).ToList();
            if (sameDim.Count == 0) continue; // only cross-dimension stock — assume sufficient

            var availableBase = sameDim.Sum(f => UnitMath.ToBase(f.Quantity, f.Unit));
            if (availableBase + 1e-6 < reqBase)
            {
                var haveInReqUnit = UnitMath.FromBase(availableBase, ri.Unit);
                shortages.Add($"{ri.Ingredient.Name} — need {Fmt(requiredQty)} {ri.Unit}, have {Fmt(haveInReqUnit)} {ri.Unit}");
            }
        }

        if (shortages.Count > 0)
            return BadRequest(new
            {
                error = $"Not enough ingredients to cook {portions} portion{(portions == 1 ? "" : "s")}:\n• " + string.Join("\n• ", shortages)
            });

        // ---- deduct from the fridge (earliest-expiry first) ----
        foreach (var ri in recipe.RecipeIngredients)
        {
            var reqDim = UnitMath.DimensionOf(ri.Unit);
            var remaining = UnitMath.ToBase(ri.Quantity * (decimal)scale, ri.Unit);

            var sameDim = fridge
                .Where(f => f.IngredientId == ri.IngredientId && UnitMath.DimensionOf(f.Unit) == reqDim)
                .OrderBy(f => f.BestBeforeDate)
                .ToList();

            foreach (var f in sameDim)
            {
                if (remaining <= 1e-6) break;
                var haveBase = UnitMath.ToBase(f.Quantity, f.Unit);
                var take = Math.Min(haveBase, remaining);
                remaining -= take;
                var leftBase = haveBase - take;
                if (leftBase <= 1e-6) _db.FridgeItems.Remove(f);
                else f.Quantity = UnitMath.FromBase(leftBase, f.Unit);
            }
        }

        var cooked = new CookedMeal
        {
            UserId = userId,
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            Portions = portions,
            CookedAt = DateTime.UtcNow
        };
        _db.CookedMeals.Add(cooked);
        await _db.SaveChangesAsync();

        return Ok(new CookedMealDto(cooked.Id, cooked.RecipeId, cooked.RecipeName, cooked.Portions, portions, cooked.CookedAt));
    }

    private static string Fmt(decimal q) =>
        q == Math.Floor(q) ? ((long)q).ToString() : Math.Round(q, 2).ToString();

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var cooked = await _db.CookedMeals.FirstOrDefaultAsync(cm => cm.Id == id && cm.UserId == userId);
        if (cooked == null) return NotFound();

        _db.CookedMeals.Remove(cooked); // cascades to its planned portions
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
