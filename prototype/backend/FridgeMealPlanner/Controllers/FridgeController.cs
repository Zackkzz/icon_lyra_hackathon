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

// The user's pantry: ingredients they own, with prices. (Route kept as /api/Fridge.)
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
            .OrderBy(f => f.Ingredient.Name)
            .ToListAsync();

        return Ok(items.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<FridgeItemDto>> Add([FromBody] AddFridgeItemRequest request)
    {
        var userId = User.GetUserId();

        var ingredient = await _db.Ingredients.FindAsync(request.IngredientId);
        if (ingredient == null)
            return NotFound(new { error = $"Ingredient {request.IngredientId} not found" });

        var saved = await AddOrMergeAsync(userId, request.IngredientId, request.Quantity, request.Unit, request.Source);
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

    // Manually set / correct an ingredient's price.
    [HttpPut("ingredient-price/{ingredientId}")]
    public async Task<ActionResult> SetPrice(int ingredientId, [FromBody] SetIngredientPriceRequest request)
    {
        var ingredient = await _db.Ingredients.FindAsync(ingredientId);
        if (ingredient == null) return NotFound();
        if (request.PricePerUnit < 0) return BadRequest(new { error = "Price must be positive." });

        ingredient.PricePerUnit = request.PricePerUnit;
        ingredient.PriceUnit = request.PriceUnit;
        await _db.SaveChangesAsync();
        return NoContent();
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

        decimal purchaseTotal = 0;
        var confirmedCount = 0;

        foreach (var item in request.Items)
        {
            var ingredientId = item.IngredientId;

            if (ingredientId is null or <= 0)
            {
                var name = (item.Name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var existingIng = await _db.Ingredients.FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());
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

            var ingredient = await _db.Ingredients.FindAsync(ingredientId);
            if (ingredient == null) continue;

            // Update the ingredient's price from this purchase (price for the quantity bought).
            if (item.Price > 0 && item.Quantity > 0)
            {
                ingredient.PricePerUnit = Math.Round(item.Price / item.Quantity, 4);
                ingredient.PriceUnit = item.Unit;
            }

            await AddOrMergeAsync(userId, ingredient.Id, item.Quantity, item.Unit, Source.Receipt);
            purchaseTotal += item.Price > 0 ? item.Price : 0;
            confirmedCount++;
        }

        // Record the spend event.
        if (confirmedCount > 0)
        {
            _db.Purchases.Add(new Purchase
            {
                UserId = userId,
                PurchasedAt = DateTime.UtcNow,
                Total = Math.Round(purchaseTotal, 2),
                ItemCount = confirmedCount
            });
        }

        await _db.SaveChangesAsync();

        var result = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .Include(f => f.Ingredient)
            .OrderBy(f => f.Ingredient.Name)
            .ToListAsync();

        return Ok(result.Select(ToDto).ToList());
    }

    // ---- helpers ----

    private async Task<FridgeItem> AddOrMergeAsync(int userId, int ingredientId, decimal quantity, Unit unit, Source source)
    {
        var existing = await _db.FridgeItems
            .FirstOrDefaultAsync(f => f.UserId == userId && f.IngredientId == ingredientId && f.Unit == unit);

        if (existing != null)
        {
            existing.Quantity += quantity;
            return existing;
        }

        var item = new FridgeItem
        {
            UserId = userId,
            IngredientId = ingredientId,
            Quantity = quantity,
            Unit = unit,
            BestBeforeDate = DateTime.UtcNow, // expiry no longer tracked; column retained
            Source = source,
            AddedAt = DateTime.UtcNow
        };
        _db.FridgeItems.Add(item);
        return item;
    }

    private static FridgeItemDto ToDto(FridgeItem f)
    {
        var ing = f.Ingredient;
        var lineValue = ing == null ? null : CostService.LineCost(ing, f.Quantity, f.Unit);
        return new FridgeItemDto(
            f.Id, f.IngredientId, ing?.Name ?? "", ing?.Category ?? "",
            f.Quantity, f.Unit, f.AddedAt, f.Source,
            ing?.PricePerUnit, ing?.PriceUnit, lineValue);
    }
}
