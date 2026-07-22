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
    DateTime BestBeforeDate,
    DateTime AddedAt,
    Source Source
);

public record AddFridgeItemRequest(
    int IngredientId,
    decimal Quantity,
    Unit Unit,
    DateTime BestBeforeDate,
    Source Source = Source.Manual
);

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
    DateTime BestBeforeDate
);

public record ScanReceiptResponse(List<ParsedReceiptItemDto> Items);

// A reviewed/edited item the user confirms into the fridge.
public record ConfirmReceiptItem(
    int? IngredientId,
    string Name,
    string Category,
    decimal Quantity,
    Unit Unit,
    DateTime BestBeforeDate
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
    List<RecipeIngredientDto> Ingredients
);

public record RecipeIngredientDto(
    int IngredientId,
    string IngredientName,
    decimal Quantity,
    Unit Unit,
    bool InFridge,
    decimal FridgeQuantity
);

public record RecipeSummaryDto(
    int Id,
    string Name,
    string Description,
    int Servings,
    int PrepTimeMinutes,
    string? ImageUrl,
    bool IsAiGenerated,
    int MatchCount,
    int TotalIngredients,
    double MatchPercentage,
    bool UsesExpiring,
    List<string> ExpiringIngredients
);

// Categorised suggestions for the Recipes tab.
public record FridgeSuggestionsDto(
    List<RecipeSummaryDto> CookFirst,
    List<RecipeSummaryDto> CanCook,
    List<RecipeSummaryDto> AlmostCanCook
);

public record GenerateRecipesRequest(int Count = 3);

// ---- Cooked meals ----

public record CookRecipeRequest(int RecipeId, int Portions);

public record CookedMealDto(
    int Id,
    int? RecipeId,
    string RecipeName,
    int Portions,
    int PortionsAvailable,
    DateTime CookedAt
);

// ---- Meal plan ----

public record MealPlanDto(
    int Id,
    DateOnly Date,
    MealType MealType,
    int? CookedMealId,
    int? RecipeId,
    string? RecipeName
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
