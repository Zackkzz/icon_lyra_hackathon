using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MealPlanController : ControllerBase
{
    private readonly AppDbContext _db;

    public MealPlanController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MealPlanDto>>> GetWeek([FromQuery] string week)
    {
        if (!DateOnly.TryParse(week, out var weekStart))
            return BadRequest("Invalid week date. Use format YYYY-MM-DD");

        var weekEnd = weekStart.AddDays(6);

        var plans = await _db.MealPlans
            .Include(mp => mp.Recipe)
            .Where(mp => mp.Date >= weekStart && mp.Date <= weekEnd)
            .OrderBy(mp => mp.Date)
            .ThenBy(mp => mp.MealType)
            .Select(mp => new MealPlanDto(
                mp.Id, mp.UserId, mp.Date, mp.MealType, mp.RecipeId, mp.Recipe != null ? mp.Recipe.Name : null
            ))
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost]
    public async Task<ActionResult<MealPlanDto>> Create([FromBody] CreateMealPlanRequest request)
    {
        if (request.RecipeId.HasValue)
        {
            var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == request.RecipeId.Value);
            if (!recipeExists)
                return NotFound($"Recipe {request.RecipeId} not found");
        }

        var plan = new MealPlan
        {
            UserId = request.UserId,
            Date = request.Date,
            MealType = request.MealType,
            RecipeId = request.RecipeId
        };

        _db.MealPlans.Add(plan);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWeek), new { week = plan.Date.ToString("yyyy-MM-dd") },
            new MealPlanDto(plan.Id, plan.UserId, plan.Date, plan.MealType, plan.RecipeId, null));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var plan = await _db.MealPlans.FindAsync(id);
        if (plan == null) return NotFound();

        _db.MealPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MealPlanDto>> Update(int id, [FromBody] CreateMealPlanRequest request)
    {
        var plan = await _db.MealPlans.FindAsync(id);
        if (plan == null) return NotFound();

        if (request.RecipeId.HasValue)
        {
            var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == request.RecipeId.Value);
            if (!recipeExists)
                return NotFound($"Recipe {request.RecipeId} not found");
        }

        plan.UserId = request.UserId;
        plan.Date = request.Date;
        plan.MealType = request.MealType;
        plan.RecipeId = request.RecipeId;

        await _db.SaveChangesAsync();

        string? recipeName = null;
        if (plan.RecipeId.HasValue)
        {
            recipeName = (await _db.Recipes.FindAsync(plan.RecipeId.Value))?.Name;
        }

        return Ok(new MealPlanDto(plan.Id, plan.UserId, plan.Date, plan.MealType, plan.RecipeId, recipeName));
    }
}
