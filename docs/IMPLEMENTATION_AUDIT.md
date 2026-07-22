# Current implementation audit

## Capability map

| Capability | Status | Evidence / limitation |
|---|---|---|
| Fridge list and expiry ordering | Implemented | Mobile sorts `bestBeforeDate`; backend returns joined ingredient data |
| Manual fridge entry | Partial | UI collects a name but submits hard-coded `ingredientId = 1` |
| Stock use/delete | Implemented | API subtracts quantity and removes empty rows |
| Receipt OCR | Not implemented | `Source.Receipt` exists, but no capture, OCR, review, or receipt tables/endpoints |
| Recipe catalogue/detail | Implemented | Five seeded recipes and normalized requirements |
| Recipe matching | Partial | API match-count endpoint exists; normal Recipes screen does not invoke it |
| Manual weekly planner | Partial | Tap-to-select works; no drag/drop, prepared meals, or slot uniqueness |
| AI weekly planning | Partial | Tool inserts random recipes directly; no constraints, proposal, or acceptance |
| Shopping generation | Partial | Persists missing same-unit quantities; route mismatch and no conversion/rounding |
| Shopping completion | Partial | `Purchased` exists; mobile toggles only local state and no update endpoint exists |
| Chat | Partial | OpenRouter tools exist; no auth, history, confirmation, or model audit records |
| Multi-user isolation | Not implemented | Plain `UserId`; global fridge/recipes; unscoped meal-plan GET |

## Integration defects to address first

1. **Backend build:** `ToolExecutor.SuggestRecipes` contains a repeated
   `.Select(e => e.GetInt32())`; the second call operates on an `int` and should
   be removed.
2. **Shopping route:** the mobile client requests `/api/shopping-list/generate`
   but `ShoppingListController` maps to `/api/shoppinglist/generate` unless an
   explicit route is added.
3. **HTTP method:** mobile `generateShoppingList` uses the generic request helper
   without `method: 'POST'`, while the controller requires POST.
4. **Manual ingredient:** Fridge UI ignores the entered name and always sends
   ingredient ID 1. Add ingredient search/create or change the request contract.
5. **Use response type:** deleting the final fridge quantity returns an object
   without the `FridgeItem` shape the TypeScript client declares.
6. **Date input:** mobile submits `YYYY-MM-DD` for a `DateTime` field. Normalize
   timezone semantics explicitly instead of relying on serializer defaults.
7. **User scoping:** `GET /api/mealplan` does not filter `UserId`; all fridge and
   recipe records are shared globally.

## Data integrity priorities

- Unique `FridgeItems (IngredientId, Unit)`.
- Unique `MealPlans (UserId, Date, MealType)` if one meal per slot is intended.
- Unique recipe requirement per recipe/ingredient or an explicit sequence key.
- Unique conversion key `(FromUnit, ToUnit, IngredientId)` with null-safe logic.
- Positive checks for stock, recipe requirements, servings, prep time, and
  multipliers.
- Relational ingredient reference on shopping items.
- Authentication-backed user/household foreign keys.

## Testing priorities

1. Compile and migration application in CI.
2. API/mobile contract tests from OpenAPI.
3. Fridge merge, expiry, use-to-zero, and concurrent-add tests.
4. Meal-slot uniqueness and user-isolation tests.
5. Unit-conversion and shopping aggregation tests.
6. AI tool argument validation, authorization, loop-limit, and confirmation
   tests.
7. OCR fixtures covering low confidence, duplicate lines, discounts, weights,
   and non-food receipt entries once OCR is implemented.

## Recommended next implementation slice

Complete a vertical “verified manual week” before OCR:

1. Fix the seven integration defects above.
2. Add stable authentication and household ownership.
3. Add constraints/migrations and validation.
4. Make tap-to-plan, shopping generation, and purchased-state persistence work
   end-to-end.
5. Add integration tests.

This creates a trustworthy foundation for receipt OCR and AI plan proposals.

