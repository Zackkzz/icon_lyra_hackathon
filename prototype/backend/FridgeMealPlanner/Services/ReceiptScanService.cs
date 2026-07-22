using System.Text.Json;
using System.Text.Json.Serialization;
using FridgeMealPlanner.Data;
using FridgeMealPlanner.DTOs;
using FridgeMealPlanner.Enums;
using Microsoft.EntityFrameworkCore;

namespace FridgeMealPlanner.Services;

public class ReceiptScanService
{
    private readonly AppDbContext _db;
    private readonly OpenRouterService _openRouter;

    public ReceiptScanService(AppDbContext db, OpenRouterService openRouter)
    {
        _db = db;
        _openRouter = openRouter;
    }

    private static readonly string[] UnitNames = Enum.GetNames<Unit>();

    // Detect the image format from the leading base64 bytes (magic numbers), so the data
    // URI's media type matches the actual image (strict providers reject a mismatch).
    private static string SniffMediaType(string b64)
    {
        if (b64.StartsWith("iVBORw0KGgo")) return "image/png";
        if (b64.StartsWith("/9j/")) return "image/jpeg";
        if (b64.StartsWith("R0lGOD")) return "image/gif";
        if (b64.StartsWith("UklGR")) return "image/webp";
        return "image/jpeg";
    }

    private static object BuildSchema() => new
    {
        type = "object",
        properties = new
        {
            items = new
            {
                type = "array",
                description = "Grocery/food line items found on the receipt.",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Human-friendly food/ingredient name, not the raw receipt code." },
                        quantity = new { type = "number", description = "Amount purchased. Use 1 if unknown." },
                        unit = new { type = "string", @enum = UnitNames, description = "Best-guess unit; use Pieces for whole items." },
                        price = new { type = "number", description = "The price printed at the end of this line (total paid for the item). Use 0 if not visible." }
                    },
                    required = new[] { "name", "quantity", "unit", "price" },
                    additionalProperties = false
                }
            }
        },
        required = new[] { "items" },
        additionalProperties = false
    };

    private class OcrResult
    {
        [JsonPropertyName("items")] public List<OcrItem> Items { get; set; } = new();
    }

    private class OcrItem
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("quantity")] public decimal Quantity { get; set; } = 1;
        [JsonPropertyName("unit")] public string Unit { get; set; } = "Pieces";
        [JsonPropertyName("price")] public decimal Price { get; set; }
    }

    public async Task<List<ParsedReceiptItemDto>> ScanAsync(string imageBase64)
    {
        if (!_openRouter.IsConfigured)
            throw new InvalidOperationException("Receipt scanning is not configured (missing OPENROUTER_API_KEY).");

        // Accept both bare base64 and full data URIs. Claude validates the declared media
        // type against the actual bytes, so sniff the real format from the base64 prefix.
        var dataUri = imageBase64.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? imageBase64
            : $"data:{SniffMediaType(imageBase64)};base64,{imageBase64}";

        var systemPrompt =
            "You read a photo of a grocery receipt and extract the food items that were purchased, so they can be added to a kitchen fridge inventory.\n\n" +
            "HOW TO READ THE RECEIPT\n" +
            "- Each purchased item is one row that STARTS with a quantity number (how many were bought) and ENDS with a price (e.g. $6.00, $18.70).\n" +
            "- A line that does NOT end with a price is a wrapped continuation of the item directly above it — merge it into that item. Names and sizes often wrap onto the next line.\n" +
            "- Use the price at the end of a row to find real item boundaries: never split one priced product into two items, and never merge two separately priced items into one.\n" +
            "- Ignore everything that is not a purchased food item: store name, date/time, subtotal, tax, total, cash, change, barcodes, bags, and non-food products.\n\n" +
            "QUANTITY AND UNIT (what actually goes in the fridge)\n" +
            "- Capture the real amount from the product's printed size, NOT the leading line count.\n" +
            "- If a weight or volume is printed (e.g. '500g', '2L', '1kg', '250g'), use that number with its unit: Grams, Kilograms, Ml, or Litres. If the leading count is more than 1, multiply it (two 120g bags = 240 Grams).\n" +
            "- If the item is naturally counted in whole pieces (eggs, fruit, cans), use Pieces with the number of pieces (a carton of 12 eggs = 12 Pieces).\n" +
            "- If a weight is given as a range (e.g. '1.3kg-1.7kg'), use the midpoint as a decimal (1.5).\n" +
            "- Decimals are allowed for quantity.\n" +
            "- Use a short, clean ingredient name ('Chicken Breast', 'Eggs', 'Butter', 'Spinach', 'Cherry Tomatoes', 'Milk', 'Greek Yoghurt', 'Feta').\n\n" +
            "PRICE\n" +
            "- Capture the price printed at the end of each line as `price` (the total paid for that item). Use 0 only if no price is visible.\n\n" +
            "EXAMPLES (a '/' marks where the line wrapped)\n" +
            "'1 Salted Butter 500g            $7.00'                          -> { name: 'Butter', quantity: 500, unit: 'Grams', price: 7.00 }\n" +
            "'2 Baby Leaf Spinach 120g        $3.30'                          -> { name: 'Spinach', quantity: 240, unit: 'Grams', price: 3.30 }\n" +
            "'1 Full Cream Milk 2L            $3.55'                          -> { name: 'Milk', quantity: 2, unit: 'Litres', price: 3.55 }\n" +
            "'1 Simply Eggs 12 Extra Large / Cage Free Eggs 700g   $6.00'     -> { name: 'Eggs', quantity: 12, unit: 'Pieces', price: 6.00 }\n" +
            "'1 Chicken Breast Fillet 1.3kg / 1.7kg               $18.70'     -> { name: 'Chicken Breast', quantity: 1.5, unit: 'Kilograms', price: 18.70 }\n" +
            "'1 Greek Style / Fetta 200g      $3.20'                          -> { name: 'Feta', quantity: 200, unit: 'Grams', price: 3.20 }";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = "Extract the purchased food items from this receipt into structured data. Return one entry per priced item." },
                    new { type = "image_url", image_url = new { url = dataUri } }
                }
            }
        };

        var contentJson = await _openRouter.CompleteToolCallAsync(
            messages,
            toolName: "record_receipt_items",
            toolDescription: "Record the food items you read from the receipt so they can be added to the fridge. One entry per priced line item.",
            parametersSchema: BuildSchema(),
            useVisionModel: true);

        var result = JsonSerializer.Deserialize<OcrResult>(contentJson)
            ?? throw new InvalidOperationException("Could not parse receipt.");

        var ingredients = await _db.Ingredients
            .Select(i => new { i.Id, i.Name, i.Category })
            .ToListAsync();

        var parsed = new List<ParsedReceiptItemDto>();

        foreach (var item in result.Items)
        {
            var name = (item.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var unit = Enum.TryParse<Unit>(item.Unit, true, out var u) ? u : Unit.Pieces;
            var qty = item.Quantity <= 0 ? 1 : item.Quantity;
            var price = item.Price < 0 ? 0 : item.Price;

            // Best-effort match to a known ingredient: exact, then contains either direction.
            var match =
                ingredients.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? ingredients.FirstOrDefault(i =>
                        name.Contains(i.Name, StringComparison.OrdinalIgnoreCase) ||
                        i.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

            parsed.Add(new ParsedReceiptItemDto(
                RawName: name,
                IngredientId: match?.Id,
                SuggestedName: match?.Name ?? name,
                Category: match?.Category ?? "Other",
                Quantity: qty,
                Unit: unit,
                Price: price
            ));
        }

        return parsed;
    }
}
