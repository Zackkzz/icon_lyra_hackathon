using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SpendingController : ControllerBase
{
    private readonly AppDbContext _db;

    public SpendingController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<SpendingResponse>> GetWeekly()
    {
        var userId = User.GetUserId();

        var purchases = await _db.Purchases
            .Where(p => p.UserId == userId)
            .Select(p => new { p.PurchasedAt, p.Total })
            .ToListAsync();

        var cooked = await _db.CookedMeals
            .Where(cm => cm.UserId == userId)
            .Select(cm => new { cm.CookedAt, cm.RecipeName, cm.Portions, cm.Cost })
            .ToListAsync();

        var weeks = new SortedDictionary<DateOnly, WeekAcc>(
            Comparer<DateOnly>.Create((a, b) => b.CompareTo(a))); // newest first

        WeekAcc Week(DateOnly wk)
        {
            if (!weeks.TryGetValue(wk, out var acc)) weeks[wk] = acc = new WeekAcc();
            return acc;
        }

        foreach (var p in purchases)
        {
            var acc = Week(WeekStart(p.PurchasedAt));
            acc.Spent += p.Total;
            acc.Purchases += 1;
        }

        foreach (var m in cooked.OrderByDescending(x => x.CookedAt))
        {
            var acc = Week(WeekStart(m.CookedAt));
            acc.PreppedCost += m.Cost;
            acc.Meals.Add(new PreppedMealDto(m.RecipeName, m.Portions, m.Cost, m.CookedAt));
        }

        var weekDtos = weeks
            .Select(kv => new WeekSpendingDto(
                kv.Key,
                Math.Round(kv.Value.Spent, 2),
                kv.Value.Purchases,
                Math.Round(kv.Value.PreppedCost, 2),
                kv.Value.Meals))
            .ToList();

        return Ok(new SpendingResponse(Math.Round(purchases.Sum(p => p.Total), 2), weekDtos));
    }

    private static DateOnly WeekStart(DateTime dt)
    {
        var d = DateOnly.FromDateTime(dt);
        var diff = ((int)d.DayOfWeek + 6) % 7; // Monday = 0
        return d.AddDays(-diff);
    }

    private sealed class WeekAcc
    {
        public decimal Spent;
        public int Purchases;
        public decimal PreppedCost;
        public List<PreppedMealDto> Meals { get; } = new();
    }
}
