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
    private const double AlmostThreshold = 70.0;

    private readonly AppDbContext _db;
    private readonly RecipeGenerationService _generator;

    public RecipesController(AppDbContext db, RecipeGenerationService generator)
    {
        _db = db;
        _generator = generator;
    }

    [HttpGet]
    public async Task<ActionResult<List<RecipeSummaryDto>>> GetAll()
    {
        var userId = User.GetUserId();
        var bookmarks = await BookmarkIds(userId);
        var recipes = await VisibleRecipes(userId)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .ToListAsync();

        var result = recipes
            .Select(r => Summary(r, new HashSet<int>(), bookmarks))
            .ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecipeDto>> GetById(int id)
    {
        var userId = User.GetUserId();

        var recipe = await _db.Recipes
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id && (r.UserId == null || r.UserId == userId));
        if (recipe == null) return NotFound();

        var fridge = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .GroupBy(f => f.IngredientId)
            .Select(g => new { IngredientId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.IngredientId, x => x.Quantity);

        var bookmarked = await _db.RecipeBookmarks.AnyAsync(b => b.UserId == userId && b.RecipeId == id);
        var (total, priced, _) = CostService.RecipeCost(recipe);

        return Ok(new RecipeDto(
            recipe.Id, recipe.Name, recipe.Description, recipe.Instructions,
            recipe.Servings, recipe.PrepTimeMinutes, recipe.ImageUrl, recipe.IsAiGenerated,
            bookmarked,
            CostService.CostPerServing(recipe),
            priced > 0 ? total : null,
            priced,
            recipe.RecipeIngredients.Select(ri => new RecipeIngredientDto(
                ri.IngredientId, ri.Ingredient.Name, ri.Quantity, ri.Unit,
                fridge.ContainsKey(ri.IngredientId),
                fridge.TryGetValue(ri.IngredientId, out var q) ? q : 0,
                ri.Ingredient == null ? null : CostService.LineCost(ri.Ingredient, ri.Quantity, ri.Unit)
            )).ToList()
        ));
    }

    // Recipes grouped for the Recipes tab: saved, cookable from pantry, and the rest.
    [HttpGet("for-fridge")]
    public async Task<ActionResult<FridgeSuggestionsDto>> ForFridge()
    {
        var userId = User.GetUserId();

        var inFridge = (await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .Select(f => f.IngredientId)
            .ToListAsync()).ToHashSet();

        var bookmarks = await BookmarkIds(userId);

        var recipes = await VisibleRecipes(userId)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .ToListAsync();

        var summaries = recipes.Select(r => Summary(r, inFridge, bookmarks)).ToList();

        var bookmarked = summaries.Where(s => s.IsBookmarked)
            .OrderBy(s => s.Name).ToList();

        var canCook = summaries
            .Where(s => !s.IsBookmarked && s.TotalIngredients > 0 && s.MatchCount == s.TotalIngredients)
            .OrderBy(s => s.CostPerServing ?? decimal.MaxValue)
            .ToList();

        var more = summaries
            .Where(s => !s.IsBookmarked && !(s.TotalIngredients > 0 && s.MatchCount == s.TotalIngredients))
            .OrderByDescending(s => s.MatchPercentage)
            .ThenBy(s => s.CostPerServing ?? decimal.MaxValue)
            .ToList();

        return Ok(new FridgeSuggestionsDto(bookmarked, canCook, more));
    }

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

    // Toggle a bookmark; returns the new state.
    [HttpPost("{id}/bookmark")]
    public async Task<ActionResult> ToggleBookmark(int id)
    {
        var userId = User.GetUserId();
        var exists = await _db.Recipes.AnyAsync(r => r.Id == id && (r.UserId == null || r.UserId == userId));
        if (!exists) return NotFound();

        var existing = await _db.RecipeBookmarks.FirstOrDefaultAsync(b => b.UserId == userId && b.RecipeId == id);
        if (existing != null)
        {
            _db.RecipeBookmarks.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { bookmarked = false });
        }

        _db.RecipeBookmarks.Add(new RecipeBookmark { UserId = userId, RecipeId = id, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return Ok(new { bookmarked = true });
    }

    private IQueryable<Recipe> VisibleRecipes(int userId) =>
        _db.Recipes.Where(r => r.UserId == null || r.UserId == userId);

    private async Task<HashSet<int>> BookmarkIds(int userId) =>
        (await _db.RecipeBookmarks.Where(b => b.UserId == userId).Select(b => b.RecipeId).ToListAsync()).ToHashSet();

    private static RecipeSummaryDto Summary(Recipe r, HashSet<int> inFridge, HashSet<int> bookmarks)
    {
        var total = r.RecipeIngredients.Count;
        var matchCount = r.RecipeIngredients.Count(ri => inFridge.Contains(ri.IngredientId));
        var pct = total > 0 ? Math.Round((double)matchCount / total * 100, 0) : 0;
        return new RecipeSummaryDto(
            r.Id, r.Name, r.Description, r.Servings, r.PrepTimeMinutes, r.ImageUrl,
            r.IsAiGenerated, bookmarks.Contains(r.Id), CostService.CostPerServing(r),
            matchCount, total, pct);
    }
}
