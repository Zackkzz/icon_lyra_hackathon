using System.Globalization;
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

    [HttpGet("trend")]
    public async Task<ActionResult<SpendingTrendResponse>> GetTrend(
        [FromQuery] string period = "week",
        [FromQuery] int utcOffsetMinutes = 0)
    {
        if (utcOffsetMinutes is < -840 or > 840)
            return BadRequest(new { error = "utcOffsetMinutes must be between -840 and 840." });

        if (!Enum.TryParse<TrendPeriod>(period, true, out var selectedPeriod))
            return BadRequest(new { error = "period must be day, week, or month." });

        var userId = User.GetUserId();
        var localToday = DateOnly.FromDateTime(DateTime.UtcNow.AddMinutes(utcOffsetMinutes));
        var currentStart = BucketStart(localToday, selectedPeriod);
        var pointCount = selectedPeriod switch
        {
            TrendPeriod.Day => 7,
            TrendPeriod.Week => 8,
            TrendPeriod.Month => 6,
            _ => throw new ArgumentOutOfRangeException()
        };
        var firstStart = Shift(currentStart, selectedPeriod, -(pointCount - 1));
        var endExclusive = Shift(currentStart, selectedPeriod, 1);

        var queryStartUtc = LocalBoundaryToUtc(firstStart, utcOffsetMinutes);
        var queryEndUtc = LocalBoundaryToUtc(endExclusive, utcOffsetMinutes);

        var purchases = await _db.Purchases
            .Where(p => p.UserId == userId &&
                        p.PurchasedAt >= queryStartUtc &&
                        p.PurchasedAt < queryEndUtc)
            .Select(p => new { p.PurchasedAt, p.Total })
            .ToListAsync();

        var buckets = new Dictionary<DateOnly, TrendAcc>();
        for (var i = 0; i < pointCount; i++)
            buckets[Shift(firstStart, selectedPeriod, i)] = new TrendAcc();

        foreach (var purchase in purchases)
        {
            var localDate = DateOnly.FromDateTime(
                purchase.PurchasedAt.ToUniversalTime().AddMinutes(utcOffsetMinutes));
            var bucket = BucketStart(localDate, selectedPeriod);
            if (!buckets.TryGetValue(bucket, out var acc)) continue;
            acc.Spent += purchase.Total;
            acc.Purchases++;
        }

        var points = buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new SpendingTrendPointDto(
                kv.Key,
                PointLabel(kv.Key, selectedPeriod),
                Math.Round(kv.Value.Spent, 2),
                kv.Value.Purchases,
                kv.Key == currentStart))
            .ToList();

        var current = buckets[currentStart];
        var previousStart = Shift(currentStart, selectedPeriod, -1);
        var previous = buckets.TryGetValue(previousStart, out var previousAcc)
            ? previousAcc
            : new TrendAcc();
        var changePercentage = previous.Spent > 0
            ? Math.Round((current.Spent - previous.Spent) / previous.Spent * 100, 1)
            : (decimal?)null;

        return Ok(new SpendingTrendResponse(
            selectedPeriod.ToString().ToLowerInvariant(),
            CurrentPeriodLabel(selectedPeriod),
            currentStart,
            endExclusive.AddDays(-1),
            Math.Round(current.Spent, 2),
            Math.Round(previous.Spent, 2),
            changePercentage,
            Math.Round(points.Sum(p => p.Spent), 2),
            current.Purchases,
            points));
    }

    private static DateOnly WeekStart(DateTime dt)
    {
        var d = DateOnly.FromDateTime(dt);
        var diff = ((int)d.DayOfWeek + 6) % 7; // Monday = 0
        return d.AddDays(-diff);
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-diff);
    }

    private static DateOnly BucketStart(DateOnly date, TrendPeriod period) => period switch
    {
        TrendPeriod.Day => date,
        TrendPeriod.Week => WeekStart(date),
        TrendPeriod.Month => new DateOnly(date.Year, date.Month, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    private static DateOnly Shift(DateOnly date, TrendPeriod period, int amount) => period switch
    {
        TrendPeriod.Day => date.AddDays(amount),
        TrendPeriod.Week => date.AddDays(amount * 7),
        TrendPeriod.Month => date.AddMonths(amount),
        _ => throw new ArgumentOutOfRangeException(nameof(period))
    };

    private static DateTime LocalBoundaryToUtc(DateOnly date, int utcOffsetMinutes)
    {
        var localBoundary = date.ToDateTime(TimeOnly.MinValue);
        return DateTime.SpecifyKind(localBoundary.AddMinutes(-utcOffsetMinutes), DateTimeKind.Utc);
    }

    private static string PointLabel(DateOnly start, TrendPeriod period) => period switch
    {
        TrendPeriod.Day => start.ToString("ddd", CultureInfo.InvariantCulture),
        TrendPeriod.Week => start.ToString("d MMM", CultureInfo.InvariantCulture),
        TrendPeriod.Month => start.ToString("MMM", CultureInfo.InvariantCulture),
        _ => ""
    };

    private static string CurrentPeriodLabel(TrendPeriod period) => period switch
    {
        TrendPeriod.Day => "Today",
        TrendPeriod.Week => "This week",
        TrendPeriod.Month => "This month",
        _ => "Current period"
    };

    private sealed class WeekAcc
    {
        public decimal Spent;
        public int Purchases;
        public decimal PreppedCost;
        public List<PreppedMealDto> Meals { get; } = new();
    }

    private sealed class TrendAcc
    {
        public decimal Spent;
        public int Purchases;
    }

    private enum TrendPeriod
    {
        Day,
        Week,
        Month
    }
}
