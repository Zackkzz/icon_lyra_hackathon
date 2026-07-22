using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.Services;

public enum Dimension { Mass, Volume, Count }

/// <summary>
/// Converts quantities to a canonical base unit within a dimension so that
/// amounts in compatible units (e.g. grams vs kilograms, ml vs litres) can be
/// compared and subtracted. Cross-dimension amounts (e.g. pieces vs grams) are
/// not convertible and are handled leniently by callers.
/// </summary>
public static class UnitMath
{
    // base units: Mass=grams, Volume=ml, Count=pieces
    public static (Dimension Dim, double Factor) Info(Unit unit) => unit switch
    {
        Unit.Grams => (Dimension.Mass, 1),
        Unit.Kilograms => (Dimension.Mass, 1000),
        Unit.Ounces => (Dimension.Mass, 28.3495),
        Unit.Pounds => (Dimension.Mass, 453.592),

        Unit.Ml => (Dimension.Volume, 1),
        Unit.Litres => (Dimension.Volume, 1000),
        Unit.Cups => (Dimension.Volume, 240),
        Unit.Tbsp => (Dimension.Volume, 15),
        Unit.Tsp => (Dimension.Volume, 5),

        Unit.Pieces => (Dimension.Count, 1),
        _ => (Dimension.Count, 1),
    };

    public static Dimension DimensionOf(Unit unit) => Info(unit).Dim;

    // Convert a quantity to its dimension's base amount.
    public static double ToBase(decimal qty, Unit unit) => (double)qty * Info(unit).Factor;

    // Convert a base amount back into a specific unit.
    public static decimal FromBase(double baseAmount, Unit unit) => (decimal)(baseAmount / Info(unit).Factor);
}
