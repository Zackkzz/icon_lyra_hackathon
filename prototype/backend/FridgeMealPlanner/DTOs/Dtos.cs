using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.DTOs;

// ---- Auth ----

public record RegisterRequest(string Email, string Password, string? DisplayName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, int UserId, string Email, string DisplayName);

// ---- Fridge ----

public record FridgeItemDto(
    int Id,
    int IngredientId,
    string IngredientName,
    string Category,
    decimal Quantity,
    Unit Unit,
    DateTime AddedAt,
    Source Source,
    decimal? UnitPrice,   // price per PriceUnit of the ingredient
    Unit? PriceUnit,
    decimal? LineValue    // approx value of this stock (null if price unit incompatible)
);

public record AddFridgeItemRequest(
    int IngredientId,
    decimal Quantity,
    Unit Unit,
    Source Source = Source.Manual
);

// Manually set/override an ingredient's price.
public record SetIngredientPriceRequest(decimal PricePerUnit, Unit PriceUnit);

// ---- Receipt scanning (OCR) ----

public record ScanReceiptRequest(string ImageBase64);

// One line item extracted from a receipt, matched (best-effort) to a known ingredient.
public record ParsedReceiptItemDto(
    string RawName,
    int? IngredientId,
    string SuggestedName,
    string Category,
    decimal Quantity,
    Unit Unit,
    decimal Price          // total price paid for this line
);

public record ScanReceiptResponse(List<ParsedReceiptItemDto> Items);

// A reviewed/edited item the user confirms into the pantry.
public record ConfirmReceiptItem(
    int? IngredientId,
    string Name,
    string Category,
    decimal Quantity,
    Unit Unit,
    decimal Price
);

public record ConfirmReceiptRequest(List<ConfirmReceiptItem> Items);

// ---- Recipes ----

public record RecipeDto(
    int Id,
    string Name,
    string Description,
    string Instructions,
    int Servings,
    int PrepTimeMinutes,
    string? ImageUrl,
    bool IsAiGenerated,
    bool IsBookmarked,
    decimal? CostPerServing,
    decimal? TotalCost,
    int PricedIngredients,
    List<RecipeIngredientDto> Ingredients
);

public record RecipeIngredientDto(
    int IngredientId,
    string IngredientName,
    decimal Quantity,
    Unit Unit,
    bool InFridge,
    decimal FridgeQuantity,
    decimal? LineCost
);

public record RecipeSummaryDto(
    int Id,
    string Name,
    string Description,
    int Servings,
    int PrepTimeMinutes,
    string? ImageUrl,
    bool IsAiGenerated,
    bool IsBookmarked,
    decimal? CostPerServing,
    int MatchCount,
    int TotalIngredients,
    double MatchPercentage
);

// Categorised recipes for the Recipes tab.
public record FridgeSuggestionsDto(
    List<RecipeSummaryDto> Bookmarked,
    List<RecipeSummaryDto> CanCook,
    List<RecipeSummaryDto> More
);

public record GenerateRecipesRequest(int Count = 3);

// ---- Cooked meals ----

public record CookRecipeRequest(int RecipeId, int Portions);

public record CookedMealDto(
    int Id,
    int? RecipeId,
    string RecipeName,
    int Portions,
    int PortionsAvailable,   // portions not yet placed on the calendar
    decimal Cost,
    DateTime CookedAt
);

// ---- Spending ----

public record PreppedMealDto(string RecipeName, int Portions, decimal Cost, DateTime CookedAt);

public record WeekSpendingDto(
    DateOnly WeekStart,
    decimal Spent,          // grocery spend from receipts that week
    int PurchaseCount,
    decimal PreppedCost,    // ingredient cost of meals prepped that week
    List<PreppedMealDto> MealsPrepped
);

public record SpendingResponse(decimal TotalSpent, List<WeekSpendingDto> Weeks);

public record SpendingTrendPointDto(
    DateOnly StartDate,
    string Label,
    decimal Spent,
    int PurchaseCount,
    bool IsCurrent
);

public record SpendingTrendResponse(
    string Period,
    string CurrentPeriodLabel,
    DateOnly CurrentPeriodStart,
    DateOnly CurrentPeriodEnd,
    decimal CurrentSpent,
    decimal PreviousSpent,
    decimal? ChangePercentage,
    decimal VisibleTotal,
    int CurrentPurchaseCount,
    List<SpendingTrendPointDto> Points
);

// ---- Meal plan ----

public record MealPlanDto(
    int Id,
    DateOnly Date,
    MealType MealType,
    int? CookedMealId,
    int? RecipeId,
    string? RecipeName,
    decimal CostPerPortion   // ingredient cost of this one portion
);

// A planned meal is a portion of a cooked meal.
public record CreateMealPlanRequest(
    DateOnly Date,
    MealType MealType,
    int CookedMealId
);

// ---- Shopping list (kept for API completeness; not surfaced in the redesigned UI) ----

public record ShoppingListDto(
    int Id,
    DateOnly WeekStartDate,
    DateTime GeneratedAt,
    List<ShoppingListItemDto> Items
);

public record ShoppingListItemDto(
    int Id,
    string IngredientName,
    decimal Quantity,
    Unit Unit,
    bool Purchased
);

// ---- Conversions / chat ----

public record ConversionDto(
    int Id,
    Unit FromUnit,
    Unit ToUnit,
    int? IngredientId,
    decimal Multiplier
);

public record ChatRequest(string Message);
public record ChatResponse(string Response);
