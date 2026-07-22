using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
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
public class RecipesController : ControllerBase
{
    private const int ExpiringSoonDays = 4;
    private const double AlmostThreshold = 70.0;

    private readonly AppDbContext _db;
    private readonly RecipeGenerationService _generator;

    public RecipesController(AppDbContext db, RecipeGenerationService generator)
    {
        _db = db;
        _generator = generator;
    }

    // All recipes visible to this user (global + their own). Used by the Plan tab.
    [HttpGet]
    public async Task<ActionResult<List<RecipeSummaryDto>>> GetAll()
    {
        var userId = User.GetUserId();
        var recipes = await VisibleRecipes(userId)
            .Select(r => new RecipeSummaryDto(
                r.Id, r.Name, r.Description, r.Servings, r.PrepTimeMinutes, r.ImageUrl,
                r.IsAiGenerated, 0, r.RecipeIngredients.Count, 0, false, new List<string>()))
            .ToListAsync();

        return Ok(recipes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecipeDto>> GetById(int id)
    {
        var userId = User.GetUserId();

        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id && (r.UserId == null || r.UserId == userId));

        if (recipe == null) return NotFound();

        // Which ingredients does the user already have (summed per ingredient)?
        var fridge = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .GroupBy(f => f.IngredientId)
            .Select(g => new { IngredientId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.IngredientId, x => x.Quantity);

        return Ok(new RecipeDto(
            recipe.Id, recipe.Name, recipe.Description, recipe.Instructions,
            recipe.Servings, recipe.PrepTimeMinutes, recipe.ImageUrl, recipe.IsAiGenerated,
            recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto(
                ri.IngredientId, ri.Ingredient.Name, ri.Quantity, ri.Unit,
                fridge.ContainsKey(ri.IngredientId),
                fridge.TryGetValue(ri.IngredientId, out var q) ? q : 0
            )).ToList()
        ));
    }

    // Categorised suggestions based on the user's fridge.
    [HttpGet("for-fridge")]
    public async Task<ActionResult<FridgeSuggestionsDto>> ForFridge()
    {
        var userId = User.GetUserId();

        var fridge = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var inFridge = fridge.Select(f => f.IngredientId).ToHashSet();
        var expiring = fridge
            .Where(f => (f.BestBeforeDate.Date - today).TotalDays <= ExpiringSoonDays)
            .Select(f => f.IngredientId)
            .ToHashSet();

        var recipes = await VisibleRecipes(userId)
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .ToListAsync();

        var summaries = recipes.Select(r =>
        {
            var total = r.RecipeIngredients.Count;
            var matchCount = r.RecipeIngredients.Count(ri => inFridge.Contains(ri.IngredientId));
            var pct = total > 0 ? Math.Round((double)matchCount / total * 100, 0) : 0;
            var expiringNames = r.RecipeIngredients
                .Where(ri => expiring.Contains(ri.IngredientId))
                .Select(ri => ri.Ingredient.Name)
                .Distinct()
                .ToList();

            return new RecipeSummaryDto(
                r.Id, r.Name, r.Description, r.Servings, r.PrepTimeMinutes, r.ImageUrl,
                r.IsAiGenerated, matchCount, total, pct,
                expiringNames.Count > 0, expiringNames);
        }).ToList();

        // High priority: uses ingredients about to expire → cook these first.
        var cookFirst = summaries
            .Where(s => s.UsesExpiring)
            .OrderByDescending(s => s.ExpiringIngredients.Count)
            .ThenByDescending(s => s.MatchPercentage)
            .ToList();

        // Medium priority: can be cooked entirely from the fridge.
        var canCook = summaries
            .Where(s => s.TotalIngredients > 0 && s.MatchCount == s.TotalIngredients)
            .OrderByDescending(s => s.MatchCount)
            .ToList();

        // Low priority: at least 70% of ingredients on hand (but not all).
        var almostCanCook = summaries
            .Where(s => s.MatchPercentage >= AlmostThreshold && s.MatchCount < s.TotalIngredients)
            .OrderByDescending(s => s.MatchPercentage)
            .ToList();

        return Ok(new FridgeSuggestionsDto(cookFirst, canCook, almostCanCook));
    }

    // High-priority feature: generate recipes that use close-to-expiry ingredients.
    [HttpPost("generate")]
    public async Task<ActionResult<List<RecipeDto>>> Generate([FromBody] GenerateRecipesRequest? request)
    {
        var userId = User.GetUserId();
        try
        {
            var ids = await _generator.GenerateForUserAsync(userId, request?.Count ?? 3);
            if (ids.Count == 0)
                return StatusCode(502, new { error = "The generator did not return any usable recipes. Try again." });

            var created = new List<RecipeDto>();
            foreach (var id in ids)
            {
                var result = await GetById(id);
                if (result.Result is OkObjectResult ok && ok.Value is RecipeDto dto)
                    created.Add(dto);
            }
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "Recipe generation failed.", detail = ex.Message });
        }
    }

    private IQueryable<Recipe> VisibleRecipes(int userId) =>
        _db.Recipes.Where(r => r.UserId == null || r.UserId == userId);
}
