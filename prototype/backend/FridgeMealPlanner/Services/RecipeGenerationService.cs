using System.Text.Json;
using System.Text.Json.Serialization;
using FridgeMealPlanner.Data;
using FridgeMealPlanner.Enums;
using FridgeMealPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Services;

public class RecipeGenerationService
{
    private const int ExpiringSoonDays = 4;

    private readonly AppDbContext _db;
    private readonly OpenRouterService _openRouter;

    public RecipeGenerationService(AppDbContext db, OpenRouterService openRouter)
    {
        _db = db;
        _openRouter = openRouter;
    }

    // ---- LLM tool output shape (strict JSON schema) ----

    private static readonly string[] UnitNames =
        Enum.GetNames<Unit>();

    private static object BuildSchema() => new
    {
        type = "object",
        properties = new
        {
            recipes = new
            {
                type = "array",
                description = "Recipes the user can cook, prioritising ingredients that expire soon.",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string" },
                        description = new { type = "string", description = "One-sentence summary of the dish." },
                        instructions = new
                        {
                            type = "array",
                            description = "Step-by-step method: one clear, self-contained step per array item, in cooking order.",
                            items = new { type = "string" }
                        },
                        servings = new { type = "integer" },
                        prepTimeMinutes = new { type = "integer" },
                        ingredients = new
                        {
                            type = "array",
                            description = "Ingredients as references to the provided ingredient catalog.",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    ingredientId = new { type = "integer", description = "An id from the provided ingredient catalog. Only use ids that exist in the catalog." },
                                    quantity = new { type = "number" },
                                    unit = new { type = "string", @enum = UnitNames }
                                },
                                required = new[] { "ingredientId", "quantity", "unit" },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[] { "name", "description", "instructions", "servings", "prepTimeMinutes", "ingredients" },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "recipes" },
        additionalProperties = false
    };

    private class GenResult
    {
        [JsonPropertyName("recipes")] public List<GenRecipe> Recipes { get; set; } = new();
    }

    private class GenRecipe
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("instructions")] public List<string> Instructions { get; set; } = new();
        [JsonPropertyName("servings")] public int Servings { get; set; }
        [JsonPropertyName("prepTimeMinutes")] public int PrepTimeMinutes { get; set; }
        [JsonPropertyName("ingredients")] public List<GenIngredient> Ingredients { get; set; } = new();
    }

    private class GenIngredient
    {
        [JsonPropertyName("ingredientId")] public int IngredientId { get; set; }
        [JsonPropertyName("quantity")] public decimal Quantity { get; set; }
        [JsonPropertyName("unit")] public string Unit { get; set; } = "Pieces";
    }

    // Join the model's step array into a stored, numbered instruction string.
    private static string FormatInstructions(List<string>? steps)
    {
        if (steps == null || steps.Count == 0) return "";
        return string.Join("\n", steps
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select((s, i) => $"{i + 1}. {s.Trim()}"));
    }

    /// <summary>
    /// Generates and persists recipes for a user, prioritising ingredients close to expiry.
    /// Returns the ids of the newly created recipes.
    /// </summary>
    public async Task<List<int>> GenerateForUserAsync(int userId, int count)
    {
        if (!_openRouter.IsConfigured)
            throw new InvalidOperationException("The recipe generator is not configured (missing OPENROUTER_API_KEY).");

        count = Math.Clamp(count, 1, 5);

        var catalog = await _db.Ingredients
            .OrderBy(i => i.Id)
            .Select(i => new { i.Id, i.Name, i.Category })
            .ToListAsync();

        var validIds = catalog.Select(c => c.Id).ToHashSet();

        var fridge = await _db.FridgeItems
            .Where(f => f.UserId == userId)
            .Include(f => f.Ingredient)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var expiring = fridge
            .Where(f => (f.BestBeforeDate.Date - today).TotalDays <= ExpiringSoonDays)
            .OrderBy(f => f.BestBeforeDate)
            .Select(f => $"#{f.IngredientId} {f.Ingredient.Name} (qty {f.Quantity} {f.Unit}, best before {f.BestBeforeDate:yyyy-MM-dd})")
            .ToList();

        var inFridge = fridge
            .GroupBy(f => f.IngredientId)
            .Select(g =>
            {
                var unit = g.First().Unit;
                var qty = g.Where(x => x.Unit == unit).Sum(x => x.Quantity);
                return $"#{g.Key} {g.First().Ingredient.Name} — have {qty} {unit}";
            })
            .ToList();

        // Recipes already saved on the account (global + this user's), so the model has
        // full context of what exists and can avoid duplicating them.
        var existingRecipes = await _db.Recipes
            .Where(r => r.UserId == null || r.UserId == userId)
            .Select(r => r.Name)
            .ToListAsync();

        var catalogText = string.Join("\n", catalog.Select(c => $"#{c.Id}: {c.Name} [{c.Category}]"));
        var expiringText = expiring.Count > 0 ? string.Join("\n", expiring) : "(nothing expiring soon)";
        var fridgeText = inFridge.Count > 0 ? string.Join("\n", inFridge) : "(fridge is empty)";
        var existingText = existingRecipes.Count > 0
            ? string.Join("\n", existingRecipes.Select(n => $"- {n}"))
            : "(none yet)";

        var systemPrompt =
            "You are a chef that designs recipes to minimise food waste. " +
            "You are given the ingredient catalog with ids, the ingredients currently in the user's fridge, which of those expire soon, and the recipes already saved on their account. " +
            "Every ingredient you reference MUST use an id from the catalog. " +
            "STRONGLY PREFER recipes the user can cook using ONLY ingredients that are already in their fridge, so they do not have to buy anything. " +
            "Among fridge-only recipes, prioritise the ones that use up the ingredients expiring soonest. " +
            "Only if a genuinely good recipe is not possible from the fridge alone should you add extra ingredients from the catalog — and then add as few as possible (ideally at most one or two). " +
            "Do NOT duplicate recipes the account already has; design different dishes. " +
            "For any ingredient that is in the fridge, express its quantity using the SAME unit shown for that fridge item (e.g. if milk is listed in Ml, give the recipe's milk amount in Ml), so the app can measure and deduct it accurately. " +
            "Keep each ingredient's amount realistic for the recipe's serving count and no larger than what is in the fridge. " +
            "Assume basics like salt, pepper, water and cooking oil are always on hand and do not list them as ingredients. " +
            "Give each recipe clear step-by-step instructions, one step per array item.";

        var userPrompt =
            $"Ingredients in the user's fridge (prefer using ONLY these):\n{fridgeText}\n\n" +
            $"Of those, expiring soonest (use these first):\n{expiringText}\n\n" +
            $"Recipes already saved on this account (do NOT duplicate these):\n{existingText}\n\n" +
            $"Full ingredient catalog (id: name [category]) — only reference ids from here:\n{catalogText}\n\n" +
            $"Generate exactly {count} new recipes, preferring ones that need no extra shopping.";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
        };

        var contentJson = await _openRouter.CompleteToolCallAsync(
            messages,
            toolName: "save_recipes",
            toolDescription: "Save the recipes you designed for the user. Provide every ingredient as a reference to an id from the ingredient catalog.",
            parametersSchema: BuildSchema());

        var result = JsonSerializer.Deserialize<GenResult>(contentJson)
            ?? throw new InvalidOperationException("Could not parse generated recipes.");

        var createdIds = new List<int>();

        foreach (var gen in result.Recipes)
        {
            var recipe = new Recipe
            {
                Name = string.IsNullOrWhiteSpace(gen.Name) ? "Untitled Recipe" : gen.Name.Trim(),
                Description = gen.Description?.Trim() ?? "",
                Instructions = FormatInstructions(gen.Instructions),
                Servings = gen.Servings <= 0 ? 2 : gen.Servings,
                PrepTimeMinutes = gen.PrepTimeMinutes < 0 ? 0 : gen.PrepTimeMinutes,
                UserId = userId,
                IsAiGenerated = true
            };

            foreach (var ing in gen.Ingredients)
            {
                if (!validIds.Contains(ing.IngredientId)) continue; // ignore hallucinated ids
                var unit = Enum.TryParse<Unit>(ing.Unit, true, out var u) ? u : Unit.Pieces;
                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    IngredientId = ing.IngredientId,
                    Quantity = ing.Quantity <= 0 ? 1 : ing.Quantity,
                    Unit = unit
                });
            }

            if (recipe.RecipeIngredients.Count == 0) continue; // skip empty recipes

            _db.Recipes.Add(recipe);
            await _db.SaveChangesAsync();
            createdIds.Add(recipe.Id);
        }

        return createdIds;
    }
}
