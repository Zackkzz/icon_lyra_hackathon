using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FridgeController : ControllerBase
{
    private readonly AppDbContext _db;

    public FridgeController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<FridgeItemDto>>> GetAll()
    {
        var items = await _db.FridgeItems
            .Include(f => f.Ingredient)
            .Select(f => new FridgeItemDto(
                f.Id,
                f.IngredientId,
                f.Ingredient.Name,
                f.Ingredient.Category,
                f.Quantity,
                f.Unit,
                f.BestBeforeDate,
                f.AddedAt,
                f.Source
            ))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<FridgeItemDto>> Add([FromBody] AddFridgeItemRequest request)
    {
        var ingredient = await _db.Ingredients.FindAsync(request.IngredientId);
        if (ingredient == null)
            return NotFound($"Ingredient {request.IngredientId} not found");

        var existing = await _db.FridgeItems
            .FirstOrDefaultAsync(f => f.IngredientId == request.IngredientId && f.Unit == request.Unit);

        if (existing != null)
        {
            existing.Quantity += request.Quantity;
            existing.BestBeforeDate = request.BestBeforeDate > existing.BestBeforeDate
                ? request.BestBeforeDate : existing.BestBeforeDate;
        }
        else
        {
            var item = new FridgeItem
            {
                IngredientId = request.IngredientId,
                Quantity = request.Quantity,
                Unit = request.Unit,
                BestBeforeDate = request.BestBeforeDate,
                Source = request.Source,
                AddedAt = DateTime.UtcNow
            };
            _db.FridgeItems.Add(item);
        }

        await _db.SaveChangesAsync();

        var saved = await _db.FridgeItems
            .Include(f => f.Ingredient)
            .FirstAsync(f => f.IngredientId == request.IngredientId && f.Unit == request.Unit);

        return Ok(new FridgeItemDto(
            saved.Id, saved.IngredientId, saved.Ingredient.Name, saved.Ingredient.Category,
            saved.Quantity, saved.Unit, saved.BestBeforeDate, saved.AddedAt, saved.Source));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var item = await _db.FridgeItems.FindAsync(id);
        if (item == null) return NotFound();

        _db.FridgeItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/use")]
    public async Task<ActionResult<FridgeItemDto>> Use(int id, [FromQuery] decimal quantity = 1)
    {
        var item = await _db.FridgeItems
            .Include(f => f.Ingredient)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (item == null) return NotFound();

        item.Quantity -= quantity;

        if (item.Quantity <= 0)
        {
            _db.FridgeItems.Remove(item);
            await _db.SaveChangesAsync();
            return Ok(new { removed = true, message = $"Used all of {item.Ingredient.Name}. Removed from fridge." });
        }

        await _db.SaveChangesAsync();

        return Ok(new FridgeItemDto(
            item.Id, item.IngredientId, item.Ingredient.Name, item.Ingredient.Category,
            item.Quantity, item.Unit, item.BestBeforeDate, item.AddedAt, item.Source));
    }
}
