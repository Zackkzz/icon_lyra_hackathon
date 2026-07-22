using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Extensions;
using FridgeMealPlanner.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MealPlanController : ControllerBase
{
    private readonly AppDbContext _db;

    public MealPlanController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MealPlanDto>>> GetWeek([FromQuery] string week)
    {
        var userId = User.GetUserId();
        if (!DateOnly.TryParse(week, out var weekStart))
            return BadRequest(new { error = "Invalid week date. Use format YYYY-MM-DD" });

        var weekEnd = weekStart.AddDays(6);

        var plans = await _db.MealPlans
            .Where(mp => mp.UserId == userId.ToString() && mp.Date >= weekStart && mp.Date <= weekEnd)
            .Include(mp => mp.CookedMeal)
            .OrderBy(mp => mp.Date)
            .ThenBy(mp => mp.MealType)
            .Select(mp => new MealPlanDto(
                mp.Id, mp.Date, mp.MealType, mp.CookedMealId, mp.RecipeId,
                mp.CookedMeal != null ? mp.CookedMeal.RecipeName : null,
                mp.CookedMeal != null && mp.CookedMeal.Portions > 0
                    ? mp.CookedMeal.Cost / mp.CookedMeal.Portions
                    : 0m))
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost]
    public async Task<ActionResult<MealPlanDto>> Create([FromBody] CreateMealPlanRequest request)
    {
        var userId = User.GetUserId();

        var cooked = await _db.CookedMeals
            .FirstOrDefaultAsync(cm => cm.Id == request.CookedMealId && cm.UserId == userId);
        if (cooked == null)
            return NotFound(new { error = $"Cooked meal {request.CookedMealId} not found" });

        var planned = await _db.MealPlans.CountAsync(mp => mp.CookedMealId == cooked.Id);
        if (planned >= cooked.Portions)
            return BadRequest(new { error = $"No portions of {cooked.RecipeName} left to plan." });

        var plan = new MealPlan
        {
            UserId = userId.ToString(),
            Date = request.Date,
            MealType = request.MealType,
            CookedMealId = cooked.Id,
            RecipeId = cooked.RecipeId
        };

        _db.MealPlans.Add(plan);
        await _db.SaveChangesAsync();

        var perPortion = cooked.Portions > 0 ? Math.Round(cooked.Cost / cooked.Portions, 2) : 0m;
        return CreatedAtAction(nameof(GetWeek), new { week = plan.Date.ToString("yyyy-MM-dd") },
            new MealPlanDto(plan.Id, plan.Date, plan.MealType, plan.CookedMealId, plan.RecipeId,
                cooked.RecipeName, perPortion));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var plan = await _db.MealPlans.FirstOrDefaultAsync(mp => mp.Id == id && mp.UserId == userId.ToString());
        if (plan == null) return NotFound();

        _db.MealPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
