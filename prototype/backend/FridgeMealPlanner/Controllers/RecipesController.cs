using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecipesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RecipesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<RecipeSummaryDto>>> GetAll()
    {
        var recipes = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .Select(r => new RecipeSummaryDto(
                r.Id, r.Name, r.Description, r.Servings, r.PrepTimeMinutes, r.ImageUrl,
                0, r.RecipeIngredients.Count, 0
            ))
            .ToListAsync();

        return Ok(recipes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecipeDto>> GetById(int id)
    {
        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null) return NotFound();

        return Ok(new RecipeDto(
            recipe.Id, recipe.Name, recipe.Description, recipe.Instructions,
            recipe.Servings, recipe.PrepTimeMinutes, recipe.ImageUrl,
            recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto(
                ri.IngredientId, ri.Ingredient.Name, ri.Quantity, ri.Unit
            )).ToList()
        ));
    }

    [HttpGet("suggest")]
    public async Task<ActionResult<List<RecipeSummaryDto>>> Suggest([FromQuery] string ingredients)
    {
        if (string.IsNullOrWhiteSpace(ingredients))
            return BadRequest("Ingredients query parameter required (comma-separated IDs)");

        var ingredientIds = ingredients.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        if (ingredientIds.Count == 0)
            return BadRequest("No valid ingredient IDs provided");

        var recipes = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .Select(r => new
            {
                Recipe = r,
                MatchCount = r.RecipeIngredients.Count(ri => ingredientIds.Contains(ri.IngredientId)),
                Total = r.RecipeIngredients.Count
            })
            .OrderByDescending(x => x.MatchCount)
            .ThenBy(x => x.Total)
            .ToListAsync();

        var result = recipes.Select(x => new RecipeSummaryDto(
            x.Recipe.Id, x.Recipe.Name, x.Recipe.Description,
            x.Recipe.Servings, x.Recipe.PrepTimeMinutes, x.Recipe.ImageUrl,
            x.MatchCount, x.Total,
            x.Total > 0 ? Math.Round((double)x.MatchCount / x.Total * 100, 0) : 0
        )).ToList();

        return Ok(result);
    }
}
