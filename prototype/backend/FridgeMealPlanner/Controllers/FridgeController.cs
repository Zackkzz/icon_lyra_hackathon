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
public class FridgeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ReceiptScanService _receiptScan;

    public FridgeController(AppDbContext db, ReceiptScanService receiptScan)
    {
        _db = db;
        _receiptScan = receiptScan;
    }

    [HttpGet]
    public async Task<ActionResult<List<FridgeItemDto>>> GetAll()
    {
        var userId = User.GetUserId();
        var items = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .Include(f => f.Ingredient)
            .OrderBy(f => f.BestBeforeDate)
            .Select(f => new FridgeItemDto(
                f.Id, f.IngredientId, f.Ingredient.Name, f.Ingredient.Category,
                f.Quantity, f.Unit, f.BestBeforeDate, f.AddedAt, f.Source))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<FridgeItemDto>> Add([FromBody] AddFridgeItemRequest request)
    {
        var userId = User.GetUserId();

        var ingredient = await _db.Ingredients.FindAsync(request.IngredientId);
        if (ingredient == null)
            return NotFound(new { error = $"Ingredient {request.IngredientId} not found" });

        var saved = await AddOrMergeAsync(userId, request.IngredientId, request.Quantity,
            request.Unit, request.BestBeforeDate, request.Source);
        await _db.SaveChangesAsync();

        await _db.Entry(saved).Reference(f => f.Ingredient).LoadAsync();
        return Ok(ToDto(saved));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var item = await _db.FridgeItems.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
        if (item == null) return NotFound();

        _db.FridgeItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/use")]
    public async Task<ActionResult> Use(int id, [FromQuery] decimal quantity = 1)
    {
        var userId = User.GetUserId();
        var item = await _db.FridgeItems
            .Include(f => f.Ingredient)
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

        if (item == null) return NotFound();

        item.Quantity -= quantity;

        if (item.Quantity <= 0)
        {
            _db.FridgeItems.Remove(item);
            await _db.SaveChangesAsync();
            return Ok(new { removed = true, message = $"Used all of {item.Ingredient.Name}. Removed from fridge." });
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(item));
    }

    // ---- Receipt scanning (OCR) ----

    [HttpPost("scan-receipt")]
    public async Task<ActionResult<ScanReceiptResponse>> ScanReceipt([FromBody] ScanReceiptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageBase64))
            return BadRequest(new { error = "An image is required." });

        try
        {
            var items = await _receiptScan.ScanAsync(request.ImageBase64);
            return Ok(new ScanReceiptResponse(items));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "Receipt scan failed.", detail = ex.Message });
        }
    }

    [HttpPost("confirm-receipt")]
    public async Task<ActionResult<List<FridgeItemDto>>> ConfirmReceipt([FromBody] ConfirmReceiptRequest request)
    {
        var userId = User.GetUserId();
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { error = "No items to add." });

        var savedItems = new List<FridgeItem>();

        foreach (var item in request.Items)
        {
            var ingredientId = item.IngredientId;

            // Resolve or create the ingredient by name if no id was supplied.
            if (ingredientId is null or <= 0)
            {
                var name = (item.Name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var existingIng = await _db.Ingredients
                    .FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());
                if (existingIng == null)
                {
                    existingIng = new Ingredient
                    {
                        Name = name,
                        Category = string.IsNullOrWhiteSpace(item.Category) ? "Other" : item.Category
                    };
                    _db.Ingredients.Add(existingIng);
                    await _db.SaveChangesAsync();
                }
                ingredientId = existingIng.Id;
            }
            else if (!await _db.Ingredients.AnyAsync(i => i.Id == ingredientId))
            {
                continue; // skip unknown ingredient id
            }

            var saved = await AddOrMergeAsync(userId, ingredientId.Value, item.Quantity,
                item.Unit, item.BestBeforeDate, Source.Receipt);
            savedItems.Add(saved);
        }

        await _db.SaveChangesAsync();

        var result = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .Include(f => f.Ingredient)
            .OrderBy(f => f.BestBeforeDate)
            .Select(f => new FridgeItemDto(
                f.Id, f.IngredientId, f.Ingredient.Name, f.Ingredient.Category,
                f.Quantity, f.Unit, f.BestBeforeDate, f.AddedAt, f.Source))
            .ToListAsync();

        return Ok(result);
    }

    // ---- helpers ----

    private async Task<FridgeItem> AddOrMergeAsync(
        int userId, int ingredientId, decimal quantity, Unit unit, DateTime bestBefore, Source source)
    {
        // Postgres 'timestamptz' columns require UTC. Client dates arrive as
        // Kind=Unspecified, so treat them as UTC.
        bestBefore = bestBefore.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(bestBefore, DateTimeKind.Utc)
            : bestBefore.ToUniversalTime();

        var existing = await _db.FridgeItems
            .FirstOrDefaultAsync(f => f.UserId == userId && f.IngredientId == ingredientId && f.Unit == unit);

        if (existing != null)
        {
            existing.Quantity += quantity;
            if (bestBefore > existing.BestBeforeDate) existing.BestBeforeDate = bestBefore;
            return existing;
        }

        var item = new FridgeItem
        {
            UserId = userId,
            IngredientId = ingredientId,
            Quantity = quantity,
            Unit = unit,
            BestBeforeDate = bestBefore,
            Source = source,
            AddedAt = DateTime.UtcNow
        };
        _db.FridgeItems.Add(item);
        return item;
    }

    private static FridgeItemDto ToDto(FridgeItem f) => new(
        f.Id, f.IngredientId, f.Ingredient.Name, f.Ingredient.Category,
        f.Quantity, f.Unit, f.BestBeforeDate, f.AddedAt, f.Source);
}
