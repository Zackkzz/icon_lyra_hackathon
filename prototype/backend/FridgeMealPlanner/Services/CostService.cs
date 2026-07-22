using FridgeMealPlanner.Enums;
using FridgeMealPlanner.Models;

namespace FridgeMealPlanner.Services;

public static class CostService
{
    /// <summary>
    /// Cost of using <paramref name="qty"/> <paramref name="unit"/> of an ingredient,
    /// given its stored price. Returns null when the ingredient has no price or the
    /// price is measured in a different dimension (e.g. price per kg, recipe in pieces).
    /// </summary>
    public static decimal? LineCost(Ingredient ing, decimal qty, Unit unit)
    {
        if (ing.PricePerUnit is not { } pricePerUnit || ing.PriceUnit is not { } priceUnit)
            return null;
        if (UnitMath.DimensionOf(unit) != UnitMath.DimensionOf(priceUnit))
            return null;

        var qtyBase = UnitMath.ToBase(qty, unit);            // amount in dimension base
        var priceUnitBase = UnitMath.ToBase(1, priceUnit);   // base amount 1 price-unit covers
        if (priceUnitBase <= 0) return null;

        var pricePerBase = pricePerUnit / (decimal)priceUnitBase;
        return Math.Round((decimal)qtyBase * pricePerBase, 2);
    }

    // Total ingredient cost of a recipe and how many of its ingredients were priced.
    public static (decimal Total, int Priced, int TotalIngredients) RecipeCost(Recipe recipe)
    {
        decimal total = 0;
        int priced = 0;
        foreach (var ri in recipe.RecipeIngredients)
        {
            var c = ri.Ingredient == null ? null : LineCost(ri.Ingredient, ri.Quantity, ri.Unit);
            if (c is { } v) { total += v; priced++; }
        }
        return (Math.Round(total, 2), priced, recipe.RecipeIngredients.Count);
    }

    public static decimal? CostPerServing(Recipe recipe)
    {
        var (total, priced, _) = RecipeCost(recipe);
        if (priced == 0) return null;
        var servings = Math.Max(1, recipe.Servings);
        return Math.Round(total / servings, 2);
    }
}
