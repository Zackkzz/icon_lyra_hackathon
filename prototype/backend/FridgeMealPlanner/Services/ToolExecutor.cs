using System.Text.Json;
using FridgeMealPlanner.Data;
using FridgeMealPlanner.Enums;
using FridgeMealPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Services;

public class ToolExecutor
{
    private readonly AppDbContext _db;

    public ToolExecutor(AppDbContext db) => _db = db;

    public static List<object> GetToolDefinitions() => new()
    {
        new
        {
            type = "function",
            function = new
            {
                name = "get_fridge_contents",
                description = "Get all items currently in the user's fridge",
                parameters = new
                {
                    type = "object",
                    properties = new { },
                    required = Array.Empty<string>()
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "get_expiring_soon",
                description = "Get fridge items that are expiring within a certain number of days",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        days = new { type = "integer", description = "Number of days to check" }
                    },
                    required = new[] { "days" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "suggest_recipes",
                description = "Get recipe suggestions based on available ingredient IDs",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        ingredient_ids = new
                        {
                            type = "array",
                            items = new { type = "integer" },
                            description = "List of ingredient IDs available"
                        }
                    },
                    required = new[] { "ingredient_ids" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "generate_meal_plan",
                description = "Generate a meal plan for a given number of days",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        days = new { type = "integer", description = "Number of days to plan for" },
                        user_id = new { type = "string", description = "User ID (default: 'default')" }
                    },
                    required = new[] { "days" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "create_shopping_list",
                description = "Create a shopping list for a given week",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        week = new { type = "string", description = "Week start date (YYYY-MM-DD)" },
                        user_id = new { type = "string", description = "User ID (default: 'default')" }
                    },
                    required = new[] { "week" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "use_ingredient",
                description = "Deduct a quantity from a fridge item",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "integer", description = "FridgeItem ID" },
                        quantity = new { type = "number", description = "Quantity to use" }
                    },
                    required = new[] { "id", "quantity" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "add_ingredient",
                description = "Add an ingredient to the fridge",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Ingredient name" },
                        quantity = new { type = "number", description = "Quantity" },
                        unit = new { type = "string", description = "Unit (Grams, Ml, Pieces, Cups, Tbsp, Tsp)" },
                        expiry = new { type = "string", description = "Best before date (YYYY-MM-DD)" }
                    },
                    required = new[] { "name", "quantity", "unit", "expiry" }
                }
            }
        }
    };

    public async Task<string> ExecuteAsync(string toolName, string arguments)
    {
        try
        {
            using var doc = JsonDocument.Parse(arguments);
            var root = doc.RootElement;

            return toolName switch
            {
                "get_fridge_contents" => await GetFridgeContents(),
                "get_expiring_soon" => await GetExpiringSoon(root.GetProperty("days").GetInt32()),
                "suggest_recipes" => await SuggestRecipes(root),
                "generate_meal_plan" => await GenerateMealPlan(root),
                "create_shopping_list" => await CreateShoppingList(root),
                "use_ingredient" => await UseIngredient(root),
                "add_ingredient" => await AddIngredient(root),
                _ => JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" })
            };
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> GetFridgeContents()
    {
        var items = await _db.FridgeItems
            .Include(f => f.Ingredient)
            .Select(f => new
            {
                id = f.Id,
                name = f.Ingredient.Name,
                category = f.Ingredient.Category,
                quantity = f.Quantity,
                unit = f.Unit.ToString(),
                best_before = f.BestBeforeDate.ToString("yyyy-MM-dd"),
                days_until_expiry = (f.BestBeforeDate - DateTime.Now).Days
            })
            .ToListAsync();

        return JsonSerializer.Serialize(items);
    }

    private async Task<string> GetExpiringSoon(int days)
    {
        var cutoff = DateTime.Now.AddDays(days);
        var items = await _db.FridgeItems
            .Include(f => f.Ingredient)
            .Where(f => f.BestBeforeDate <= cutoff && f.BestBeforeDate >= DateTime.Now)
            .Select(f => new
            {
                id = f.Id,
                name = f.Ingredient.Name,
                quantity = f.Quantity,
                unit = f.Unit.ToString(),
                best_before = f.BestBeforeDate.ToString("yyyy-MM-dd"),
                days_until_expiry = (f.BestBeforeDate - DateTime.Now).Days
            })
            .ToListAsync();

        return JsonSerializer.Serialize(items);
    }

    private async Task<string> SuggestRecipes(JsonElement root)
    {
        var ids = root.GetProperty("ingredient_ids").EnumerateArray()
            .Select(e => e.GetInt32())
            .ToHashSet();

        var recipes = await _db.Recipes
            .Include(r => r.RecipeIngredients)
            .Select(r => new
            {
                id = r.Id,
                name = r.Name,
                description = r.Description,
                match_count = r.RecipeIngredients.Count(ri => ids.Contains(ri.IngredientId)),
                total_ingredients = r.RecipeIngredients.Count,
                missing = r.RecipeIngredients
                    .Where(ri => !ids.Contains(ri.IngredientId))
                    .Select(ri => ri.Ingredient.Name)
                    .ToList()
            })
            .Where(x => x.match_count > 0)
            .OrderByDescending(x => x.match_count)
            .Take(5)
            .ToListAsync();

        return JsonSerializer.Serialize(recipes);
    }

    private async Task<string> GenerateMealPlan(JsonElement root)
    {
        var days = root.GetProperty("days").GetInt32();
        var userId = root.TryGetProperty("user_id", out var uid) ? uid.GetString() ?? "default" : "default";

        var recipes = await _db.Recipes.ToListAsync();
        if (recipes.Count == 0)
            return JsonSerializer.Serialize(new { error = "No recipes available" });

        var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        var plans = new List<object>();
        var rng = new Random();

        for (int d = 0; d < days; d++)
        {
            var date = startDate.AddDays(d);
            var mealTypes = new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner };

            foreach (var mt in mealTypes)
            {
                var recipe = recipes[rng.Next(recipes.Count)];
                var plan = new MealPlan
                {
                    UserId = userId,
                    Date = date,
                    MealType = mt,
                    RecipeId = recipe.Id
                };
                _db.MealPlans.Add(plan);
                plans.Add(new { date = date.ToString("yyyy-MM-dd"), meal_type = mt.ToString(), recipe = recipe.Name });
            }
        }

        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new { message = $"Generated meal plan for {days} days", plans });
    }

    private async Task<string> CreateShoppingList(JsonElement root)
    {
        var week = root.GetProperty("week").GetString()!;
        var userId = root.TryGetProperty("user_id", out var uid) ? uid.GetString() ?? "default" : "default";

        if (!DateOnly.TryParse(week, out var weekStart))
            return JsonSerializer.Serialize(new { error = "Invalid date format" });

        var weekEnd = weekStart.AddDays(6);

        var mealPlans = await _db.MealPlans
            .Include(mp => mp.Recipe)
            .ThenInclude(r => r!.RecipeIngredients)
            .Where(mp => mp.UserId == userId && mp.Date >= weekStart && mp.Date <= weekEnd && mp.RecipeId != null)
            .ToListAsync();

        var required = new Dictionary<int, (string Name, decimal Qty, Unit Unit)>();
        foreach (var mp in mealPlans)
        {
            if (mp.Recipe == null) continue;
            foreach (var ri in mp.Recipe.RecipeIngredients)
            {
                if (required.ContainsKey(ri.IngredientId))
                {
                    var e = required[ri.IngredientId];
                    required[ri.IngredientId] = (e.Name, e.Qty + ri.Quantity, ri.Unit);
                }
                else
                {
                    var name = (await _db.Ingredients.FindAsync(ri.IngredientId))?.Name ?? "Unknown";
                    required[ri.IngredientId] = (name, ri.Quantity, ri.Unit);
                }
            }
        }

        var fridgeItems = await _db.FridgeItems.ToListAsync();
        var fridge = fridgeItems.ToDictionary(f => f.IngredientId, f => f);

        var missing = new List<object>();
        foreach (var (ingId, (name, qty, unit)) in required)
        {
            decimal need = qty;
            if (fridge.TryGetValue(ingId, out var fi) && fi.Unit == unit)
                need = qty - fi.Quantity;
            if (need > 0)
                missing.Add(new { ingredient = name, quantity = need, unit = unit.ToString() });
        }

        var shoppingList = new ShoppingList
        {
            UserId = userId,
            WeekStartDate = weekStart,
            GeneratedAt = DateTime.UtcNow,
            ShoppingListItems = missing.Select(m =>
            {
                var d = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(m));
                return new ShoppingListItem
                {
                    IngredientName = d.GetProperty("ingredient").GetString()!,
                    Quantity = d.GetProperty("quantity").GetDecimal(),
                    Unit = Enum.Parse<Unit>(d.GetProperty("unit").GetString()!)
                };
            }).ToList()
        };

        _db.ShoppingLists.Add(shoppingList);
        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new { message = $"Shopping list created for week of {week}", items = missing });
    }

    private async Task<string> UseIngredient(JsonElement root)
    {
        var id = root.GetProperty("id").GetInt32();
        var qty = root.GetProperty("quantity").GetDecimal();

        var item = await _db.FridgeItems.Include(f => f.Ingredient).FirstOrDefaultAsync(f => f.Id == id);
        if (item == null)
            return JsonSerializer.Serialize(new { error = "Fridge item not found" });

        item.Quantity -= qty;
        if (item.Quantity <= 0)
        {
            _db.FridgeItems.Remove(item);
            await _db.SaveChangesAsync();
            return JsonSerializer.Serialize(new { removed = true, name = item.Ingredient.Name });
        }

        await _db.SaveChangesAsync();
        return JsonSerializer.Serialize(new { id, name = item.Ingredient.Name, remaining = item.Quantity });
    }

    private async Task<string> AddIngredient(JsonElement root)
    {
        var name = root.GetProperty("name").GetString()!;
        var qty = root.GetProperty("quantity").GetDecimal();
        var unitStr = root.GetProperty("unit").GetString()!;
        var expiryStr = root.GetProperty("expiry").GetString()!;

        if (!Enum.TryParse<Unit>(unitStr, true, out var unit))
            return JsonSerializer.Serialize(new { error = $"Unknown unit: {unitStr}" });
        if (!DateTime.TryParse(expiryStr, out var expiry))
            return JsonSerializer.Serialize(new { error = "Invalid date format" });

        // Find or create ingredient
        var ingredient = await _db.Ingredients.FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());
        if (ingredient == null)
        {
            ingredient = new Ingredient { Name = name, Category = "Other" };
            _db.Ingredients.Add(ingredient);
            await _db.SaveChangesAsync();
        }

        var existing = await _db.FridgeItems
            .FirstOrDefaultAsync(f => f.IngredientId == ingredient.Id && f.Unit == unit);

        if (existing != null)
        {
            existing.Quantity += qty;
            if (expiry > existing.BestBeforeDate) existing.BestBeforeDate = expiry;
        }
        else
        {
            _db.FridgeItems.Add(new FridgeItem
            {
                IngredientId = ingredient.Id,
                Quantity = qty,
                Unit = unit,
                BestBeforeDate = expiry,
                Source = Source.Manual,
                AddedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(new { added = true, ingredient = ingredient.Name, quantity = qty, unit = unitStr });
    }
}
