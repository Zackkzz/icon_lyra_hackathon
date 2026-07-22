using FridgeMealPlanner.Enums;

namespace FridgeMealPlanner.DTOs;

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

public record RecipeDto(
    int Id,
    string Name,
    string Description,
    string Instructions,
    int Servings,
    int PrepTimeMinutes,
    string? ImageUrl,
    List<RecipeIngredientDto> Ingredients
);

public record RecipeIngredientDto(
    int IngredientId,
    string IngredientName,
    decimal Quantity,
    Unit Unit
);

public record RecipeSummaryDto(
    int Id,
    string Name,
    string Description,
    int Servings,
    int PrepTimeMinutes,
    string? ImageUrl,
    int MatchCount,
    int TotalIngredients,
    double MatchPercentage
);

public record MealPlanDto(
    int Id,
    string UserId,
    DateOnly Date,
    MealType MealType,
    int? RecipeId,
    string? RecipeName
);

public record CreateMealPlanRequest(
    string UserId,
    DateOnly Date,
    MealType MealType,
    int? RecipeId
);

public record ShoppingListDto(
    int Id,
    string UserId,
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

public record ConversionDto(
    int Id,
    Unit FromUnit,
    Unit ToUnit,
    int? IngredientId,
    decimal Multiplier
);

public record ChatRequest(string Message);

public record ChatResponse(string Response);
