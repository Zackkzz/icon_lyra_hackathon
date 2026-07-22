using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Enums;
using FridgeMealPlanner.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConversionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ConversionDto>>> GetAll()
    {
        var conversions = await _db.UnitConversions
            .Select(uc => new ConversionDto(uc.Id, uc.FromUnit, uc.ToUnit, uc.IngredientId, uc.Multiplier))
            .ToListAsync();

        return Ok(conversions);
    }

    [HttpGet("convert")]
    public async Task<ActionResult> Convert(
        [FromQuery] decimal from,
        [FromQuery] string fromUnit,
        [FromQuery] string toUnit,
        [FromQuery] int? ingredientId = null)
    {
        if (!Enum.TryParse<Unit>(fromUnit, true, out var fromU))
            return BadRequest($"Invalid fromUnit: {fromUnit}");
        if (!Enum.TryParse<Unit>(toUnit, true, out var toU))
            return BadRequest($"Invalid toUnit: {toUnit}");

        if (fromU == toU)
            return Ok(new { from, fromUnit = fromU.ToString(), toUnit = toU.ToString(), result = from });

        // Try ingredient-specific conversion first
        UnitConversion? conv = null;
        if (ingredientId.HasValue)
        {
            conv = await _db.UnitConversions
                .FirstOrDefaultAsync(uc => uc.FromUnit == fromU && uc.ToUnit == toU && uc.IngredientId == ingredientId.Value);
        }

        // Fall back to generic conversion
        if (conv == null)
        {
            conv = await _db.UnitConversions
                .FirstOrDefaultAsync(uc => uc.FromUnit == fromU && uc.ToUnit == toU && uc.IngredientId == null);
        }

        if (conv == null)
        {
            // Try reverse direction
            UnitConversion? rev = null;
            if (ingredientId.HasValue)
            {
                rev = await _db.UnitConversions
                    .FirstOrDefaultAsync(uc => uc.FromUnit == toU && uc.ToUnit == fromU && uc.IngredientId == ingredientId.Value);
            }
            if (rev == null)
            {
                rev = await _db.UnitConversions
                    .FirstOrDefaultAsync(uc => uc.FromUnit == toU && uc.ToUnit == fromU && uc.IngredientId == null);
            }

            if (rev != null)
            {
                var result = from / rev.Multiplier;
                return Ok(new { from, fromUnit = fromU.ToString(), toUnit = toU.ToString(), result = Math.Round(result, 2), reverseConversion = true });
            }

            return NotFound($"No conversion found from {fromU} to {toU}" + (ingredientId.HasValue ? $" for ingredient {ingredientId}" : ""));
        }

        var converted = from * conv.Multiplier;
        return Ok(new { from, fromUnit = fromU.ToString(), toUnit = toU.ToString(), result = Math.Round(converted, 2) });
    }
}
