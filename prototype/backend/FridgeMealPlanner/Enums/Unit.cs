namespace FridgeMealPlanner.Enums;

public enum Unit
{
    // NOTE: existing values keep their positions; new units are appended so
    // that persisted integer values (and seeded conversions) stay stable.
    Grams,
    Ml,
    Pieces,
    Cups,
    Tbsp,
    Tsp,
    Kilograms,
    Litres,
    Ounces,
    Pounds
}
