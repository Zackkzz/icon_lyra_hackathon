# FridgeMealPlanner Backend

## Stack
.NET 10 Web API, Entity Framework Core (Code First), Npgsql, PostgreSQL 16, Swashbuckle (Swagger)

## Build & Run
```bash
cd prototype/backend/FridgeMealPlanner
dotnet build
dotnet run
```

## EF Core Migrations
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## API Endpoints

### Fridge — `/api/fridge`
- `GET /` — all fridge items with ingredient name
- `POST /` — add item `{ ingredientId, quantity, unit, bestBeforeDate, source }`
- `DELETE /{id}` — remove item
- `PATCH /{id}/use?quantity=X` — deduct quantity, removes if zero

### Recipes — `/api/recipes`
- `GET /` — all recipes (summary)
- `GET /{id}` — recipe detail with ingredients
- `GET /suggest?ingredients=1,2,3` — rank by ingredient match

### Meal Plan — `/api/mealplan`
- `GET /?week=2026-07-22` — week's plans
- `POST /` — create `{ userId, date, mealType, recipeId }`
- `PUT /{id}` — update
- `DELETE /{id}` — delete

### Shopping List — `/api/shopping-list`
- `POST /generate?week=2026-07-22&userId=default` — generate from meal plan vs fridge

### Conversions — `/api/conversions`
- `GET /` — all conversions
- `GET /convert?from=200&fromUnit=Grams&toUnit=Cups&ingredientId=5`

### Chat — `/api/chat`
- `POST /` — `{ "message": "..." }` returns LLM response with real data

## Configuration
- Connection string: `ConnectionStrings__DefaultConnection` env var or appsettings.json
- OpenRouter: `OPENROUTER_API_KEY` and `OPENROUTER_MODEL` env vars
- CORS: all origins (dev)
- Swagger: enabled at `/swagger`

## Conventions
- No raw SQL — EF Core / LINQ only
- Models in `Models/`, enums in `Enums/`, DTOs in `DTOs/`
- Controllers use `[ApiController]` attribute routing
- DbContext auto-migrates on startup (`db.Database.Migrate()`)
